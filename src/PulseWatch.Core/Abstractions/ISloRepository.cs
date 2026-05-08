using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface ISloRepository
{
    Task<SloDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SloDefinition>> GetByProbeAsync(Guid probeId, CancellationToken ct = default);
    Task<IReadOnlyList<SloDefinition>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(SloDefinition definition, CancellationToken ct = default);
    Task AddMeasurementAsync(SloMeasurement measurement, CancellationToken ct = default);
    Task<SloMeasurement?> GetLatestMeasurementAsync(Guid sloDefinitionId, CancellationToken ct = default);
}
