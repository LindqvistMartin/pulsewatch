using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class ProbesApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid orgId, Guid projectId)> CreateHierarchyAsync()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Test Org", "test-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Test Project", "test-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        return (org.Id, project!.Id);
    }

    [Fact]
    public async Task Post_CreatesProbeAndReturns201()
    {
        var (_, projectId) = await CreateHierarchyAsync();

        var response = await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("API Health", "https://httpbin.org/status/200", 60));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProbeResponse>();
        body!.Name.Should().Be("API Health");
        body.IntervalSeconds.Should().Be(60);
        body.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Get_ReturnsOnlyProbesForProject()
    {
        var (_, projectId) = await CreateHierarchyAsync();
        await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("Probe A", "https://example.com", 30));
        await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("Probe B", "https://example.com/b", 60));

        var probes = await (await _client.GetAsync($"/api/v1/projects/{projectId}/probes"))
            .Content.ReadFromJsonAsync<List<ProbeResponse>>();

        probes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WrongProject_Returns404()
    {
        var (_, projectId) = await CreateHierarchyAsync();
        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("Probe", "https://example.com", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        var response = await _client.GetAsync($"/api/v1/projects/{Guid.NewGuid()}/probes/{probe!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WrongProject_DoesNotDeleteProbe()
    {
        var (_, projectId) = await CreateHierarchyAsync();
        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("Probe", "https://example.com", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        // Delete with wrong projectId
        await _client.DeleteAsync($"/api/v1/projects/{Guid.NewGuid()}/probes/{probe!.Id}");

        // Probe should still exist
        var getResponse = await _client.GetAsync($"/api/v1/projects/{projectId}/probes/{probe.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_CorrectProject_RemovesProbe()
    {
        var (_, projectId) = await CreateHierarchyAsync();
        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("Temp Probe", "https://example.com", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        await _client.DeleteAsync($"/api/v1/projects/{projectId}/probes/{probe!.Id}");

        var getResponse = await _client.GetAsync($"/api/v1/projects/{projectId}/probes/{probe.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithIntervalBelowMinimum_Returns400()
    {
        var (_, projectId) = await CreateHierarchyAsync();

        var response = await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("API", "https://example.com", 5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithBlankName_Returns400()
    {
        var (_, projectId) = await CreateHierarchyAsync();

        var response = await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/probes",
            new CreateProbeRequest("", "https://example.com", 30));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithSameIdempotencyKey_ReturnsSameProbeAndNoDuplicate()
    {
        var (_, projectId) = await CreateHierarchyAsync();
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new CreateProbeRequest("Idempotent Probe", "https://example.com/idempotent", 30);

        using var req1 = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/projects/{projectId}/probes");
        req1.Headers.Add("Idempotency-Key", idempotencyKey);
        req1.Content = JsonContent.Create(request);

        using var req2 = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/projects/{projectId}/probes");
        req2.Headers.Add("Idempotency-Key", idempotencyKey);
        req2.Content = JsonContent.Create(request);

        var resp1 = await _client.SendAsync(req1);
        var resp2 = await _client.SendAsync(req2);

        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);

        var body1 = await resp1.Content.ReadFromJsonAsync<ProbeResponse>();
        var body2 = await resp2.Content.ReadFromJsonAsync<ProbeResponse>();

        body1!.Id.Should().Be(body2!.Id, "idempotent requests must return the same resource");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var count = await db.Probes.CountAsync(p => p.ProjectId == projectId
            && p.Name == "Idempotent Probe");
        count.Should().Be(1, "idempotency key must prevent duplicate creation");
    }
}
