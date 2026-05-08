using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
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
}
