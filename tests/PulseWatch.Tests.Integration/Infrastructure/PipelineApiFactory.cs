using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace PulseWatch.Tests.Integration.Infrastructure;

public sealed class PipelineApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("pulsewatch_pipeline_test")
        .WithUsername("pulse")
        .WithPassword("test")
        .Build();

    public WireMockServer WireMock { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        WireMock = WireMockServer.Start();

        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        WireMock?.Stop();
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
            var opts = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PulseDbContext>));
            if (opts is not null) services.Remove(opts);
            services.AddDbContext<PulseDbContext>(o => o.UseNpgsql(_pg.GetConnectionString()));

            // Background services are intentionally left running for pipeline tests
        });
    }

    public async Task CleanAsync()
    {
        WireMock.Reset(); // clear stubs so they don't accumulate across tests (B7 fix)

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
        await db.Projects.ExecuteDeleteAsync();
        await db.Organizations.ExecuteDeleteAsync();
    }
}

[CollectionDefinition("Pipeline")]
public class PipelineCollection : ICollectionFixture<PipelineApiFactory> { }
