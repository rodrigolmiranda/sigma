using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Infrastructure.Persistence.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly SigmaDbContext _context;

    public MessageRepository(SigmaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Message?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Message?> GetByPlatformIdAsync(string platformMessageId, Guid channelId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.ChannelId == channelId)
            .Where(m => m.PlatformMessageId == platformMessageId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetByChannelIdAsync(Guid channelId, Guid tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.TimestampUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetRecentAsync(Guid channelId, Guid tenantId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.ChannelId == channelId)
            .Where(m => m.TimestampUtc >= since)
            .OrderBy(m => m.TimestampUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    public Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _context.Messages.Update(message);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string platformMessageId, Guid channelId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.TenantId == tenantId)
            .Where(m => m.ChannelId == channelId)
            .AnyAsync(m => m.PlatformMessageId == platformMessageId, cancellationToken);
    }
}