using Sigma.Domain.Entities;

namespace Sigma.Domain.Repositories;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Workspace>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByExternalIdAsync(string externalId, string platform, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default);
    Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}