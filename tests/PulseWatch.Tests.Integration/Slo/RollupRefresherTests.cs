using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Infrastructure.Slo;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Slo;

[Collection("Api")]
public class RollupRefresherTests(ApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(Timeout = 30_000)]
    public async Task RefreshOnceAsync_AfterHealthChecksInserted_PopulatesMatview()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var org = new Organization("RollupOrg", "rollup-org");
        var project = new Project(org.Id, "RollupProject", "rollup-proj");
        var probe = new Probe(project.Id, "RollupProbe", "https://example.com/health", 30);
        db.Organizations.Add(org);
        db.Projects.Add(project);
        db.Probes.Add(probe);
        await db.SaveChangesAsync();

        // 5 successes, 5 failures, varying response times
        var responseTimes = new[] { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 };
        for (int i = 0; i < 10; i++)
        {
            db.HealthChecks.Add(new HealthCheck(probe.Id, 200, responseTimes[i], isSuccess: i < 5));
        }
        await db.SaveChangesAsync();

        await RollupRefresher.RefreshOnceAsync(db);

        var rows = await db.Database.SqlQuery<RollupRow>($"""
            SELECT probe_id, SUM(total) AS total, SUM(success) AS success, MAX(p95_ms) AS p95_ms
            FROM health_check_1h
            WHERE probe_id = {probe.Id}
            GROUP BY probe_id
            """).ToListAsync();

        rows.Should().HaveCount(1, "matview should have one row per probe per bucket");
        var row = rows[0];
        row.total.Should().Be(10);
        row.success.Should().Be(5);
        row.p95_ms.Should().NotBeNull("p95 should be computed from the 10 response times");
    }

    private sealed class RollupRow
    {
        public Guid probe_id { get; init; }
        public long total { get; init; }
        public long success { get; init; }
        public int? p95_ms { get; init; }
    }
}
