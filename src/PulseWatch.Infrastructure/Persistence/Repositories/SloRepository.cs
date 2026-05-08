using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class SloRepository(PulseDbContext db) : ISloRepository
{
    public Task<SloDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SloDefinitions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<SloDefinition>> GetByProbeAsync(Guid probeId, CancellationToken ct = default) =>
        await db.SloDefinitions.Where(s => s.ProbeId == probeId).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<SloDefinition>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.SloDefinitions.AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(SloDefinition definition, CancellationToken ct = default)
    {
        db.SloDefinitions.Add(definition);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMeasurementAsync(SloMeasurement measurement, CancellationToken ct = default)
    {
        db.SloMeasurements.Add(measurement);
        await db.SaveChangesAsync(ct);
    }

    public Task<SloMeasurement?> GetLatestMeasurementAsync(Guid sloDefinitionId, CancellationToken ct = default) =>
        db.SloMeasurements
            .Where(m => m.SloDefinitionId == sloDefinitionId)
            .OrderByDescending(m => m.ComputedAt)
            .FirstOrDefaultAsync(ct);
}
