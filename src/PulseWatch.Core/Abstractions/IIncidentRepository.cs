using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IIncidentRepository
{
    Task<Incident?> GetOpenByProbeAsync(Guid probeId, CancellationToken ct = default);
    Task<IReadOnlyList<Incident>> GetByProbeAsync(Guid probeId, CancellationToken ct = default);
    Task AddAsync(Incident incident, CancellationToken ct = default);
    Task AddUpdateAsync(IncidentUpdate update, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
