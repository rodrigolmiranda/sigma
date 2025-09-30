using Sigma.Domain.Entities;

namespace Sigma.Domain.Repositories;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Channel>> GetByWorkspaceIdAsync(Guid workspaceId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Channel?> GetByExternalIdAsync(string externalId, Guid workspaceId, Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Channel channel, CancellationToken cancellationToken = default);
    Task UpdateAsync(Channel channel, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
}