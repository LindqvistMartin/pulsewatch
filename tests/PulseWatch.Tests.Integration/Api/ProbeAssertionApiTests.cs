using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class ProbeAssertionApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_WithAssertions_Returns201AndPersistsAssertions()
    {
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("AssertOrg", "assert-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("AssertProject", "assert-proj"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
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
        var probe = await response.Content.ReadFromJsonAsync<ProbeResponse>();
        probe.Should().NotBeNull();

        // I7 fix: verify assertions actually reached the DB
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var assertions = await db.ProbeAssertions.Where(a => a.ProbeId == probe!.Id).ToListAsync();
        assertions.Should().HaveCount(2);
        assertions.Should().Contain(a => a.Type == AssertionType.StatusCode && a.ExpectedValue == "200");
        assertions.Should().Contain(a => a.Type == AssertionType.LatencyMs && a.ExpectedValue == "1000");
    }

    [Fact]
    public async Task Post_WithInvalidAssertionType_Returns400()
    {
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("BadOrg", "bad-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("BadProject", "bad-proj"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest(
                "Bad Type Probe", "https://example.com", 60,
                Assertions: new[] { new CreateAssertionRequest("UnknownType", "Equals", "200") }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Post_WithInvalidOperator_Returns400()
    {
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("BadOpOrg", "bad-op-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("BadOpProject", "bad-op-proj"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest(
                "Bad Op Probe", "https://example.com", 60,
                Assertions: new[] { new CreateAssertionRequest("StatusCode", "NotAnOperator", "200") }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithJsonPathAssertionMissingExpression_Returns400()
    {
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("JsonPathOrg", "jsonpath-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("JsonPathProject", "jsonpath-proj"));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest(
                "JsonPath No Expression", "https://example.com", 60,
                Assertions: new[]
                {
                    // JsonPath assertion with null JsonPathExpression — should be rejected
                    new CreateAssertionRequest("JsonPath", "Equals", "ok")
                }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithNonExistentProjectId_Returns404()
    {
        var nonExistentProjectId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{nonExistentProjectId}/probes",
            new CreateProbeRequest("Some Probe", "https://example.com", 60));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<T> PostAndRead<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
