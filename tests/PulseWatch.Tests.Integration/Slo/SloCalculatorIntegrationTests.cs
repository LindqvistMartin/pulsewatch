using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Infrastructure.Slo;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Slo;

[Collection("Api")]
public class SloCalculatorIntegrationTests(ApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private SloCalculator BuildCalculator() => new(
        factory.Services.GetRequiredService<IServiceScopeFactory>(),
        factory.Services.GetRequiredService<ILogger<SloCalculator>>());

    private async Task<(Guid probeId, Guid sloId, PulseDbContext db, IServiceScope scope)>
        SeedAsync(double target, int total, int successes)
    {
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var org = new Organization("SloCalcOrg", "slo-calc-org");
        var project = new Project(org.Id, "SloCalcProject", "slo-calc-proj");
        var probe = new Probe(project.Id, "SloCalcProbe", "https://example.com/health", 30);
        db.Organizations.Add(org);
        db.Projects.Add(project);
        db.Probes.Add(probe);
        await db.SaveChangesAsync();

        for (int i = 0; i < total; i++)
        {
            db.HealthChecks.Add(new HealthCheck(probe.Id, 200, 100 + i,
                isSuccess: i < successes));
        }
        var slo = new SloDefinition(probe.Id, target, 7);
        db.SloDefinitions.Add(slo);
        await db.SaveChangesAsync();

        await RollupRefresher.RefreshOnceAsync(db);

        return (probe.Id, slo.Id, db, scope);
    }

    [Fact(Timeout = 30_000)]
    public async Task ComputeAllAsync_WritesSnapshotWithCorrectAvailability()
    {
        var (_, sloId, db, scope) = await SeedAsync(target: 99.0, total: 100, successes: 95);
        using var _ = scope;

        await BuildCalculator().ComputeAllAsync(CancellationToken.None);

        db.ChangeTracker.Clear();
        var measurement = await db.SloMeasurements
            .Where(m => m.SloDefinitionId == sloId)
            .OrderByDescending(m => m.ComputedAt)
            .FirstOrDefaultAsync();

        measurement.Should().NotBeNull("calculator must write a SloMeasurement");
        measurement!.AvailabilityPct.Should().BeApproximately(95.0, 0.5);
        measurement.BurnRate.Should().BeGreaterThan(1.0,
            "5% failure rate exceeds the 1% error budget for a 99.0% SLO");
    }

    [Fact(Timeout = 30_000)]
    public async Task ComputeAllAsync_WhenAvailabilityDropsBelowTarget_OpensIncident()
    {
        // 50% failure — severe breach of 99.9% SLO
        var (probeId, _, db, scope) = await SeedAsync(target: 99.9, total: 100, successes: 50);
        using var _ = scope;

        await BuildCalculator().ComputeAllAsync(CancellationToken.None);

        db.ChangeTracker.Clear();
        var openIncident = await db.Incidents
            .Where(i => i.ProbeId == probeId && i.ClosedAt == null)
            .FirstOrDefaultAsync();

        openIncident.Should().NotBeNull("availability below target should open an incident");
        openIncident!.AutoDetected.Should().BeTrue();
    }

    [Fact(Timeout = 30_000)]
    public async Task ComputeAllAsync_WhenProbeRecovers_ClosesOpenIncident()
    {
        // First: breach to open the incident
        var (probeId, _, db, scope) = await SeedAsync(target: 99.9, total: 100, successes: 50);
        using var __ = scope;
        await BuildCalculator().ComputeAllAsync(CancellationToken.None);

        // Wipe health checks, insert 100% successes
        await db.HealthChecks.ExecuteDeleteAsync();
        for (int i = 0; i < 100; i++)
            db.HealthChecks.Add(new HealthCheck(probeId, 200, 100, isSuccess: true));
        await db.SaveChangesAsync();
        await RollupRefresher.RefreshOnceAsync(db);

        // Second compute run — should close the incident
        await BuildCalculator().ComputeAllAsync(CancellationToken.None);

        db.ChangeTracker.Clear();
        var incident = await db.Incidents
            .Where(i => i.ProbeId == probeId)
            .OrderByDescending(i => i.OpenedAt)
            .FirstOrDefaultAsync();

        incident.Should().NotBeNull();
        incident!.ClosedAt.Should().NotBeNull("incident must be closed after availability recovers");
    }
}
