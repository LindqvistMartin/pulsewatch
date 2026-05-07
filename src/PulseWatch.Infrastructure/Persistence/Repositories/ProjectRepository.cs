using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(PulseDbContext db) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        await db.Projects.AsNoTracking().Where(p => p.OrganizationId == organizationId).ToListAsync(ct);

    public Task<Project?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default) =>
        db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, ct);

    public async Task AddAsync(Project project, CancellationToken ct = default)
    {
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
    }
}
