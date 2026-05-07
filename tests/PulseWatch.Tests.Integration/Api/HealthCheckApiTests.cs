using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class HealthCheckApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid projectId, Guid probeId)> CreateProbeAsync()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Org", "org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();
        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Proj", "proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();
        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("API", "https://example.com", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        return (project.Id, probe!.Id);
    }

    [Fact]
    public async Task GetChecks_NoChecks_ReturnsEmpty()
    {
        var (projectId, probeId) = await CreateProbeAsync();

        var response = await _client.GetAsync($"/api/v1/projects/{projectId}/probes/{probeId}/checks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checks = await response.Content.ReadFromJsonAsync<List<HealthCheckResponse>>();
        checks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChecks_WithExistingChecks_ReturnsThem()
    {
        var (projectId, probeId) = await CreateProbeAsync();

        // Directly insert a health check record via DbContext
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        db.HealthChecks.Add(new HealthCheck(probeId, 200, 142, true));
        db.HealthChecks.Add(new HealthCheck(probeId, 200, 98, true));
        await db.SaveChangesAsync();

        var checks = await (await _client.GetAsync($"/api/v1/projects/{projectId}/probes/{probeId}/checks"))
            .Content.ReadFromJsonAsync<List<HealthCheckResponse>>();

        checks.Should().HaveCount(2);
        checks!.All(c => c.IsSuccess).Should().BeTrue();
    }

    [Fact]
    public async Task GetChecks_WrongProject_Returns404()
    {
        var (_, probeId) = await CreateProbeAsync();

        var response = await _client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/probes/{probeId}/checks");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHealthzReady_Returns200()
    {
        var response = await _client.GetAsync("/healthz/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
