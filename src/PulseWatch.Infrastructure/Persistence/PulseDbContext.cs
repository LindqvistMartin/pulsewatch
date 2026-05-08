using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence;

public sealed class PulseDbContext(DbContextOptions<PulseDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Probe> Probes => Set<Probe>();
    public DbSet<ProbeAssertion> ProbeAssertions => Set<ProbeAssertion>();
    public DbSet<HealthCheck> HealthChecks => Set<HealthCheck>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SloDefinition> SloDefinitions => Set<SloDefinition>();
    public DbSet<SloMeasurement> SloMeasurements => Set<SloMeasurement>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentUpdate> IncidentUpdates => Set<IncidentUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PulseDbContext).Assembly);
    }
}
