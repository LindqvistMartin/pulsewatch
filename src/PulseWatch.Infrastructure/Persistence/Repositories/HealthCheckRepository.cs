using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class HealthCheckRepository(PulseDbContext db) : IHealthCheckRepository
{
    public async Task<IReadOnlyList<HealthCheck>> GetByProbeAsync(Guid probeId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await db.HealthChecks
            .Where(h => h.ProbeId == probeId && h.CheckedAt >= from && h.CheckedAt <= to)
            .OrderByDescending(h => h.CheckedAt)
            .Take(500)
            .ToListAsync(ct);

}
