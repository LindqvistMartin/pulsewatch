using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<Project?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
}
