using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PulseWatch.Tests.Integration.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("pulsewatch_test")
        .WithUsername("pulse")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        // Touch the server to initialise the app, then migrate
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _pg.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _pg.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace DbContext registration with the test container's connection
            var opts = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PulseDbContext>));
            if (opts is not null) services.Remove(opts);
            services.AddDbContext<PulseDbContext>(o => o.UseNpgsql(_pg.GetConnectionString()));

            // Disable background probe scheduler/workers to avoid noise during tests
            var bgServices = services
                .Where(d => d.ImplementationType?.FullName?.StartsWith("PulseWatch.Infrastructure.Probes") == true)
                .ToList();
            foreach (var d in bgServices) services.Remove(d);

            // Disable SLO background services
            var bgSloServices = services
                .Where(d => d.ImplementationType?.FullName?.StartsWith("PulseWatch.Infrastructure.Slo") == true)
                .ToList();
            foreach (var d in bgSloServices) services.Remove(d);

            // Also disable OutboxRelay (lives in Api, not caught by the Infrastructure.Probes filter above).
            // OutboxRelay is internal, so we match by assembly + short type name instead of typeof().
            var apiAssembly = typeof(Program).Assembly;
            var outboxRelay = services.SingleOrDefault(d =>
                d.ImplementationType?.Assembly == apiAssembly &&
                d.ImplementationType.Name == "OutboxRelay");
            if (outboxRelay is not null) services.Remove(outboxRelay);
        });
    }

    public async Task CleanAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        await db.SloMeasurements.ExecuteDeleteAsync();
        await db.SloDefinitions.ExecuteDeleteAsync();
        await db.IncidentUpdates.ExecuteDeleteAsync();
        await db.Incidents.ExecuteDeleteAsync();
        await db.OutboxMessages.ExecuteDeleteAsync();
        await db.HealthChecks.ExecuteDeleteAsync();
        await db.ProbeAssertions.ExecuteDeleteAsync();
        await db.Probes.ExecuteDeleteAsync();
        await db.StatusPages.ExecuteDeleteAsync();
        await db.Projects.ExecuteDeleteAsync();
        await db.Organizations.ExecuteDeleteAsync();
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }
