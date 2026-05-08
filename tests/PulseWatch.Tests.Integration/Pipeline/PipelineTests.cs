using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PulseWatch.Tests.Integration.Pipeline;

[Collection("Pipeline")]
public class PipelineTests(PipelineApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Probe_ExecutesAndRecordsSuccessfulHealthCheck()
    {
        // Arrange
        factory.WireMock
            .Given(Request.Create().WithPath("/health").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("ok"));

        var probeUrl = $"{factory.WireMock.Urls[0]}/health";

        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("PipelineOrg", "pipeline-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("PipelineProject", "pipeline-proj"));

        await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("Pipeline Health", probeUrl, 15));

        // Act: poll DB until a health check appears (scheduler fires within 5s)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var check = await WaitForHealthCheckAsync(db, timeout: TimeSpan.FromSeconds(20));

        // Assert
        check.Should().NotBeNull("scheduler should have executed the probe within 20 seconds");
        check!.IsSuccess.Should().BeTrue();
        check.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Probe_WithStatusCodeAssertion_RecordsFailureWhenMismatch()
    {
        // WireMock returns 200, assertion expects 201 → should fail
        factory.WireMock
            .Given(Request.Create().WithPath("/strict").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var probeUrl = $"{factory.WireMock.Urls[0]}/strict";

        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("AssertPipelineOrg", "assert-pipeline-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("AssertPipelineProject", "assert-pipeline-proj"));

        await _client.PostAsJsonAsync(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest(
                "Strict Probe", probeUrl, 15,
                Assertions: new[] { new CreateAssertionRequest("StatusCode", "Equals", "201") }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var check = await WaitForHealthCheckAsync(db, timeout: TimeSpan.FromSeconds(20));

        check.Should().NotBeNull("probe should have executed within 20 seconds");
        check!.IsSuccess.Should().BeFalse("status code 200 should fail the assertion expecting 201");
        check.FailureReason.Should().Contain("StatusCode");
    }

    private async Task<T> PostAndRead<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<PulseWatch.Core.Entities.HealthCheck?> WaitForHealthCheckAsync(
        PulseDbContext db, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            var check = await db.HealthChecks
                .OrderByDescending(h => h.CheckedAt)
                .FirstOrDefaultAsync();
            if (check is not null) return check;
        }
        return null;
    }
}
