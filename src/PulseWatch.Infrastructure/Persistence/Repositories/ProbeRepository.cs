using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class ProbeRepository(PulseDbContext db) : IProbeRepository
{
    public Task<Probe?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Probes.Include(p => p.Assertions).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Probe>> GetActiveAsync(CancellationToken ct = default) =>
        await db.Probes.Include(p => p.Assertions).Where(p => p.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyList<Probe>> GetByProjectAsync(Guid projectId, CancellationToken ct = default) =>
        await db.Probes.Where(p => p.ProjectId == projectId).ToListAsync(ct);

    public async Task AddAsync(Probe probe, CancellationToken ct = default)
    {
        db.Probes.Add(probe);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid projectId, Guid id, CancellationToken ct = default)
    {
        await db.Probes.Where(p => p.Id == id && p.ProjectId == projectId).ExecuteDeleteAsync(ct);
    }

    public async Task MarkCheckedAsync(Guid id, CancellationToken ct = default)
    {
        var probe = await db.Probes.FindAsync([id], ct);
        if (probe is null) return;
        probe.RecordChecked();
        await db.SaveChangesAsync(ct);
    }
}
