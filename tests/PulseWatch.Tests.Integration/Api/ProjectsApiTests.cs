using System.Net;
using System.Net.Http.Json;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class ProjectsApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> CreateOrgAsync(string name = "TestOrg", string slug = "test-org")
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest(name, slug)))
            .Content.ReadFromJsonAsync<OrganizationResponse>();
        return org!.Id;
    }

    [Fact]
    public async Task Post_CreatesProjectAndReturns201()
    {
        var orgId = await CreateOrgAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/organizations/{orgId}/projects",
            new CreateProjectRequest("Backend Services", "backend"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        body!.Name.Should().Be("Backend Services");
        body.Slug.Should().Be("backend");
        body.OrganizationId.Should().Be(orgId);
        response.Headers.Location!.ToString().Should().Contain(body.Id.ToString());
    }

    [Fact]
    public async Task Get_ReturnsOnlyProjectsForOrganization()
    {
        var orgId1 = await CreateOrgAsync("Org1", "org1");
        var orgId2 = await CreateOrgAsync("Org2", "org2");

        await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId1}/projects",
            new CreateProjectRequest("Project A", "project-a"));
        await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId1}/projects",
            new CreateProjectRequest("Project B", "project-b"));
        await _client.PostAsJsonAsync($"/api/v1/organizations/{orgId2}/projects",
            new CreateProjectRequest("Project C", "project-c"));

        var projects = await (await _client.GetAsync($"/api/v1/organizations/{orgId1}/projects"))
            .Content.ReadFromJsonAsync<List<ProjectResponse>>();

        projects.Should().HaveCount(2);
        projects!.All(p => p.OrganizationId == orgId1).Should().BeTrue();
    }

    [Fact]
    public async Task Get_UnknownOrganization_ReturnsEmpty()
    {
        var response = await _client.GetAsync($"/api/v1/organizations/{Guid.NewGuid()}/projects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();
        projects.Should().BeEmpty();
    }
}
