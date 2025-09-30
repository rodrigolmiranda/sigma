using Sigma.Domain.Entities;

namespace Sigma.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Message?> GetByPlatformIdAsync(string platformMessageId, Guid channelId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetByChannelIdAsync(Guid channelId, Guid tenantId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetRecentAsync(Guid channelId, Guid tenantId, DateTime since, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string platformMessageId, Guid channelId, Guid tenantId, CancellationToken cancellationToken = default);
}