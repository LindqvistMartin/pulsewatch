using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Pipeline;

[Collection("Api")]
public class OutboxAtomicityTests(ApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(Timeout = 30_000)]
    public async Task TransactionRollback_WritesNeitherHealthCheckNorOutboxMessage()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        var org = new Organization("AtomicOrg", "atomic-org");
        var project = new Project(org.Id, "AtomicProject", "atomic-proj");
        var probe = new Probe(project.Id, "AtomicProbe", "https://example.com/health", 30);
        db.Organizations.Add(org);
        db.Projects.Add(project);
        db.Probes.Add(probe);
        await db.SaveChangesAsync();

        var probeId = probe.Id;

        // Simulate ProbeWorker write path: begin transaction, add both entities, then roll back
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.HealthChecks.Add(new HealthCheck(probeId, 200, 150, isSuccess: true));
            db.OutboxMessages.Add(new OutboxMessage("HealthCheckRecorded", """{"probeId":"test"}"""));
            await db.SaveChangesAsync();
            // Roll back instead of commit — simulates a failed CommitAsync
            await tx.RollbackAsync();
        }

        // Detach all tracked entities so the next query hits the DB fresh
        db.ChangeTracker.Clear();

        var healthChecks = await db.HealthChecks.Where(h => h.ProbeId == probeId).ToListAsync();
        var outboxMessages = await db.OutboxMessages.CountAsync();

        healthChecks.Should().BeEmpty("transaction was rolled back — no health check should persist");
        outboxMessages.Should().Be(0, "transaction was rolled back — no outbox message should persist");
    }
}
