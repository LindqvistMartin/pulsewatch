using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Infrastructure.Slo;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class SloApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid probeId, Guid projectId)> CreateHierarchyAsync()
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("SLO Org", "slo-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("SLO Project", "slo-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("SLO Probe", "https://example.com/health", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        return (probe!.Id, project.Id);
    }

    [Fact]
    public async Task PostSlo_ReturnsCreated()
    {
        var (probeId, projectId) = await CreateHierarchyAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/probes/{probeId}/slos",
            new CreateSloRequest(99.9, 30));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SloDefinitionResponse>();
        body!.TargetAvailabilityPct.Should().Be(99.9);
        body.WindowDays.Should().Be(30);
        body.LatestMeasurement.Should().BeNull();
    }

    [Fact]
    public async Task GetSlos_AfterCalculatorRun_ReturnsMeasurementSnapshot()
    {
        var (probeId, projectId) = await CreateHierarchyAsync();

        // Seed 100 health checks directly (99 success, 1 failure)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        for (int i = 0; i < 100; i++)
        {
            db.HealthChecks.Add(new HealthCheck(probeId, 200, 100 + i, isSuccess: i != 50));
        }
        await db.SaveChangesAsync();

        // POST SLO definition
        var sloResp = await (await _client.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/probes/{probeId}/slos",
            new CreateSloRequest(99.9, 7)))
            .Content.ReadFromJsonAsync<SloDefinitionResponse>();
        sloResp.Should().NotBeNull();

        // Trigger rollup refresh and SLO calculation synchronously
        await RollupRefresher.RefreshOnceAsync(db);
        var calculator = new SloCalculator(
            factory.Services.GetRequiredService<IServiceScopeFactory>(),
            factory.Services.GetRequiredService<ILogger<SloCalculator>>());
        await calculator.ComputeAllAsync(CancellationToken.None);

        // Verify measurement was written
        var slos = await (await _client.GetAsync(
            $"/api/v1/projects/{projectId}/probes/{probeId}/slos"))
            .Content.ReadFromJsonAsync<List<SloDefinitionResponse>>();

        slos.Should().HaveCount(1);
        var measurement = slos![0].LatestMeasurement;
        measurement.Should().NotBeNull();
        measurement!.AvailabilityPct.Should().BeApproximately(99.0, 0.1);
        measurement.BurnRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSlos_WrongProject_ReturnsNotFound()
    {
        var (probeId, _) = await CreateHierarchyAsync();
        var wrongProjectId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/api/v1/projects/{wrongProjectId}/probes/{probeId}/slos");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
