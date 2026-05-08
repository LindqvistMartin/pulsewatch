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

    [Fact(Timeout = 25_000)]
    public async Task Probe_ExecutesAndRecordsSuccessfulHealthCheck()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/health").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("ok"));

        var probeUrl = $"{factory.WireMock.Urls[0]}/health";

        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("PipelineOrg", "pipeline-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("PipelineProject", "pipeline-proj"));

        // Capture probeId from response to scope the wait query (B6 fix)
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("Pipeline Health", probeUrl, 15));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var check = await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(20));

        check.Should().NotBeNull("scheduler should have executed the probe within 20 seconds");
        check!.IsSuccess.Should().BeTrue();
        check.StatusCode.Should().Be(200);
    }

    [Fact(Timeout = 25_000)]
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

        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest(
                "Strict Probe", probeUrl, 15,
                Assertions: new[] { new CreateAssertionRequest("StatusCode", "Equals", "201") }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var check = await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(20));

        check.Should().NotBeNull("probe should have executed within 20 seconds");
        check!.IsSuccess.Should().BeFalse("status code 200 should fail the assertion expecting 201");
        check.FailureReason.Should().Contain("StatusCode");
    }

    [Fact(Timeout = 30_000)]
    public async Task OutboxRelay_AfterProbeExecution_MarksMessagesProcessed()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/outbox-relay").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var probeUrl = $"{factory.WireMock.Urls[0]}/outbox-relay";
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("OutboxOrg", "outbox-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("OutboxProject", "outbox-proj"));
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("OutboxProbe", probeUrl, 15));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(20));

        var processed = await WaitForOutboxProcessedAsync(db, TimeSpan.FromSeconds(15));
        processed.Should().BeTrue("OutboxRelay should mark all messages processed within 15 seconds");
    }

    [Fact(Timeout = 30_000)]
    public async Task Probe_WithMultipleAssertions_WhenOneFails_RecordsFailureWithCorrectReason()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/and-logic").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"status":"error"}""")
                .WithHeader("Content-Type", "application/json"));

        var probeUrl = $"{factory.WireMock.Urls[0]}/and-logic";
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("AndOrg", "and-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("AndProject", "and-proj"));
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("AND Logic Probe", probeUrl, 15,
                Assertions: new[]
                {
                    new CreateAssertionRequest("StatusCode", "Equals", "200"),
                    new CreateAssertionRequest("JsonPath", "Equals", "ok", "$.status")
                }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var check = await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(20));

        check.Should().NotBeNull();
        check!.IsSuccess.Should().BeFalse("JsonPath assertion expects 'ok' but body has 'error'");
        check.FailureReason.Should().Contain("JsonPath");
    }

    [Fact(Timeout = 30_000)]
    public async Task Probe_WhenTargetTimesOut_RecordsFailure()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/slow").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(15)));

        var probeUrl = $"{factory.WireMock.Urls[0]}/slow";
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("TimeoutOrg", "timeout-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("TimeoutProject", "timeout-proj"));
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("Timeout Probe", probeUrl, 15));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var check = await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(25));

        check.Should().NotBeNull("probe should have executed and recorded a failure");
        check!.IsSuccess.Should().BeFalse();
        check.FailureReason.Should().Contain("timed out",
            "ProbeWorker records timeout as 'Probe timed out after Ns'");
    }

    [Fact(Timeout = 20_000)]
    public async Task Probe_WhenHostDoesNotExist_RecordsFailure()
    {
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("DnsOrg", "dns-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("DnsProject", "dns-proj"));
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("DNS Probe",
                "https://this-host-does-not-exist-pulsewatch.invalid/health", 15));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var check = await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(15));

        check.Should().NotBeNull("probe should have recorded a DNS failure");
        check!.IsSuccess.Should().BeFalse();
        check.FailureReason.Should().NotBeNullOrEmpty("HttpRequestException message should be captured");
    }

    [Fact(Timeout = 15_000)]
    public async Task ProbeScheduler_WhenProbeIsInactive_DoesNotExecuteIt()
    {
        // Insert inactive probe directly — avoids race between API create and deactivation
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var org = new PulseWatch.Core.Entities.Organization("InactiveOrg", "inactive-org");
        var project = new PulseWatch.Core.Entities.Project(org.Id, "InactiveProject", "inactive-proj");
        var probe = new PulseWatch.Core.Entities.Probe(project.Id, "Inactive Probe",
            "https://example.com/health", 15);
        probe.Deactivate();
        db.Organizations.Add(org);
        db.Projects.Add(project);
        db.Probes.Add(probe);
        await db.SaveChangesAsync();

        await Task.Delay(TimeSpan.FromSeconds(10));

        db.ChangeTracker.Clear();
        var count = await db.HealthChecks.CountAsync(h => h.ProbeId == probe.Id);
        count.Should().Be(0, "inactive probe must never be scheduled");
    }

    [Fact(Timeout = 30_000)]
    public async Task ProbeScheduler_DoesNotReExecuteProbeBeforeIntervalElapsed()
    {
        factory.WireMock
            .Given(Request.Create().WithPath("/interval-gate").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var probeUrl = $"{factory.WireMock.Urls[0]}/interval-gate";
        var org = await PostAndRead<OrganizationResponse>("/api/v1/organizations",
            new CreateOrganizationRequest("IntervalOrg", "interval-org"));
        var project = await PostAndRead<ProjectResponse>(
            $"/api/v1/organizations/{org.Id}/projects",
            new CreateProjectRequest("IntervalProject", "interval-proj"));
        var probe = await PostAndRead<ProbeResponse>(
            $"/api/v1/projects/{project.Id}/probes",
            new CreateProbeRequest("Interval Probe", probeUrl, 30));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        // Wait for exactly one check
        await WaitForHealthCheckAsync(db, probe.Id, timeout: TimeSpan.FromSeconds(20));

        // Wait well below the 30s interval
        await Task.Delay(TimeSpan.FromSeconds(5));

        var count = await db.HealthChecks.CountAsync(h => h.ProbeId == probe.Id);
        count.Should().Be(1, "scheduler must respect IntervalSeconds and not re-execute before 30s");
    }

    [Fact(Timeout = 15_000)]
    public async Task OutboxRelay_WhenPayloadMissingProjectId_MarksMessageProcessedWithoutRetry()
    {
        // Valid JSON but missing ProjectId — after the TryGetProperty fix OutboxRelay logs
        // an error and marks the message processed rather than retrying indefinitely.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var poison = new PulseWatch.Core.Entities.OutboxMessage(
            "HealthCheckRecorded", """{"ProbeId":"00000000-0000-0000-0000-000000000000"}""");
        db.OutboxMessages.Add(poison);
        await db.SaveChangesAsync();

        // Use a LINQ projection (not FindAsync) to always query the DB, not the change tracker.
        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            var processedAt = await db.OutboxMessages
                .Where(m => m.Id == poison.Id)
                .Select(m => m.ProcessedAt)
                .FirstOrDefaultAsync();
            if (processedAt != null) return; // test passed
        }

        Assert.Fail("OutboxRelay did not mark broken-payload message as processed within 12 seconds");
    }

    private async Task<T> PostAndRead<T>(string url, object body)
    {
        var response = await _client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    // B6 fix: scoped to a specific probeId — no longer picks up HealthChecks from other tests
    private static async Task<PulseWatch.Core.Entities.HealthCheck?> WaitForHealthCheckAsync(
        PulseDbContext db, Guid probeId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            var check = await db.HealthChecks
                .Where(h => h.ProbeId == probeId)
                .OrderByDescending(h => h.CheckedAt)
                .FirstOrDefaultAsync();
            if (check is not null) return check;
        }
        return null;
    }

    private static async Task<bool> WaitForOutboxProcessedAsync(PulseDbContext db, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            // LINQ queries always hit the DB; no change-tracker clear needed.
            var pending = await db.OutboxMessages.CountAsync(m => m.ProcessedAt == null);
            if (pending == 0) return true;
        }
        return false;
    }
}
