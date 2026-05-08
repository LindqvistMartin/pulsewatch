using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class IncidentRepository(PulseDbContext db) : IIncidentRepository
{
    public Task<Incident?> GetOpenByProbeAsync(Guid probeId, CancellationToken ct = default) =>
        db.Incidents.FirstOrDefaultAsync(i => i.ProbeId == probeId && i.ClosedAt == null, ct);

    public async Task<IReadOnlyList<Incident>> GetByProbeAsync(Guid probeId, CancellationToken ct = default) =>
        await db.Incidents
            .Include(i => i.Updates)
            .Where(i => i.ProbeId == probeId)
            .OrderByDescending(i => i.OpenedAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task AddAsync(Incident incident, CancellationToken ct = default)
    {
        db.Incidents.Add(incident);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddUpdateAsync(IncidentUpdate update, CancellationToken ct = default)
    {
        db.IncidentUpdates.Add(update);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
