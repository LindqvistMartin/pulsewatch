using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class DataIntegrityTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteOrganization_CascadesProjectsAndProbes()
    {
        // Arrange
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Cascade Org", "cascade-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Cascade Project", "cascade-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("Cascade Probe", "https://example.com/health", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        // Also insert a HealthCheck directly
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        db.HealthChecks.Add(new HealthCheck(probe!.Id, 200, 50, true));
        await db.SaveChangesAsync();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/organizations/{org.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PulseDbContext>();
        (await verifyDb.Projects.AnyAsync(p => p.OrganizationId == org.Id)).Should().BeFalse();
        (await verifyDb.Probes.AnyAsync(p => p.ProjectId == project.Id)).Should().BeFalse();
        (await verifyDb.HealthChecks.AnyAsync(h => h.ProbeId == probe.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrganization_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/organizations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostProject_DuplicateSlug_ReturnsConflict()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Dup Org", "dup-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var first = await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("First", "dup-slug"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync($"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("Second", "dup-slug"));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
