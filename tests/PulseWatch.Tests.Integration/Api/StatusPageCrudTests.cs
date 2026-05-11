using System.Net;
using System.Net.Http.Json;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class StatusPageCrudTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid projectId, Guid probeId)> CreateHierarchyAsync()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("SP Org", "sp-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("SP Project", "sp-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("SP Probe", "https://example.com/health", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        return (project.Id, probe!.Id);
    }

    [Fact]
    public async Task Post_CreatesStatusPageAndReturns201()
    {
        var (projectId, probeId) = await CreateHierarchyAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/status-pages",
            new CreateStatusPageRequest("my-status", "My Status", "", [probeId]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StatusPageResponse>();
        body!.Slug.Should().Be("my-status");
        body.Title.Should().Be("My Status");
        body.ProjectId.Should().Be(projectId);
        body.ProbeIds.Should().ContainSingle().Which.Should().Be(probeId);
        response.Headers.Location!.ToString().Should().Contain(body.Id.ToString());
    }

    [Fact]
    public async Task Get_AfterCreate_ReturnsStatusPage()
    {
        var (projectId, probeId) = await CreateHierarchyAsync();

        await _client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/status-pages",
            new CreateStatusPageRequest("list-status", "List Status", "", [probeId]));

        var pages = await (await _client.GetAsync($"/api/v1/projects/{projectId}/status-pages"))
            .Content.ReadFromJsonAsync<List<StatusPageResponse>>();

        pages.Should().HaveCount(1);
        pages![0].Slug.Should().Be("list-status");
        pages[0].ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Delete_RemovesStatusPage()
    {
        var (projectId, probeId) = await CreateHierarchyAsync();

        var created = await (await _client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/status-pages",
            new CreateStatusPageRequest("del-status", "Del Status", "", [probeId])))
            .Content.ReadFromJsonAsync<StatusPageResponse>();

        var deleteResp = await _client.DeleteAsync(
            $"/api/v1/projects/{projectId}/status-pages/{created!.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publicResp = await _client.GetAsync("/public/status/del-status");
        publicResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_NonExistentProject_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{Guid.NewGuid()}/status-pages",
            new CreateStatusPageRequest("missing-proj", "Missing", "", []));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_DuplicateSlug_ReturnsConflict()
    {
        var (projectId, _) = await CreateHierarchyAsync();
        var req = new CreateStatusPageRequest("dupe-slug", "Dupe Status", "", []);

        await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/status-pages", req);
        var response = await _client.PostAsJsonAsync($"/api/v1/projects/{projectId}/status-pages", req);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
