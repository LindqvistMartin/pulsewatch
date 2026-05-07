using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IHealthCheckRepository
{
    Task<IReadOnlyList<HealthCheck>> GetByProbeAsync(Guid probeId, DateTime from, DateTime to, CancellationToken ct = default);
}
