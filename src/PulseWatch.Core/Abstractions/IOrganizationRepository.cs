using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IOrganizationRepository
{
    Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken ct = default);
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Organization organization, CancellationToken ct = default);
}
