using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IHealthCheckRepository
{
    Task<IReadOnlyList<HealthCheck>> GetByProbeAsync(Guid probeId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(HealthCheck check, CancellationToken ct = default);
}
