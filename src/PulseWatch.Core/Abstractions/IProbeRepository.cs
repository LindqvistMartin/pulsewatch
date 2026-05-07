using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IProbeRepository
{
    Task<Probe?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Probe>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Probe>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task AddAsync(Probe probe, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
