using Microsoft.EntityFrameworkCore;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationRepository(PulseDbContext db) : IOrganizationRepository
{
    public async Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default) =>
        await db.Organizations.AsNoTracking().ToListAsync(ct);

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Organization organization, CancellationToken ct = default)
    {
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);
    }
}
