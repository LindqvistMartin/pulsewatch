using System.Net;
using System.Net.Http.Json;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class ProbeAssertionApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithAssertions_Returns201()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("AssertOrg", "assert-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("AssertProject", "assert-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest(
                "Probe With Assertions",
                "https://example.com",
                60,
                Assertions: new[]
                {
                    new CreateAssertionRequest("StatusCode", "Equals", "200"),
                    new CreateAssertionRequest("LatencyMs", "LessThan", "1000")
                }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithInvalidAssertionType_Returns400()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("BadOrg", "bad-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("BadProject", "bad-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest(
                "Bad Type Probe",
                "https://example.com",
                60,
                Assertions: new[] { new CreateAssertionRequest("UnknownType", "Equals", "200") }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithInvalidOperator_Returns400()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("BadOpOrg", "bad-op-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("BadOpProject", "bad-op-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest(
                "Bad Op Probe",
                "https://example.com",
                60,
                Assertions: new[] { new CreateAssertionRequest("StatusCode", "NotAnOperator", "200") }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
