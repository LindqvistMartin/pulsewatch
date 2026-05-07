using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Probes;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Infrastructure.Persistence.Repositories;
using PulseWatch.Infrastructure.Probes;

namespace PulseWatch.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PulseDbContext>(o => o.UseNpgsql(connectionString));

        services.AddScoped<IProbeRepository, ProbeRepository>();
        services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();

        var channel = Channel.CreateBounded<ProbeJob>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        services.AddSingleton(channel);
        services.AddHostedService<ProbeScheduler>();
        for (int i = 0; i < 4; i++)
            services.AddHostedService<ProbeWorker>();

        services.AddHttpClient("probe", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("PulseWatch/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        });

        return services;
    }
}
