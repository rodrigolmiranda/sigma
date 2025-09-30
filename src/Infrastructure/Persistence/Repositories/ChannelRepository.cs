using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Infrastructure.Persistence.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly SigmaDbContext _context;

    public ChannelRepository(SigmaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Channel?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Include(c => c.Messages.Take(100)) // Limit messages to avoid loading too much data
            .FirstOrDefaultAsync(c => c.Id == id && EF.Property<Guid>(c, "TenantId") == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Channel>> GetByWorkspaceIdAsync(Guid workspaceId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Where(c => c.WorkspaceId == workspaceId && EF.Property<Guid>(c, "TenantId") == tenantId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Channel?> GetByExternalIdAsync(string externalId, Guid workspaceId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Where(c => c.ExternalId == externalId)
            .Where(c => c.WorkspaceId == workspaceId)
            .Where(c => EF.Property<Guid>(c, "TenantId") == tenantId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        // Set the tenant ID shadow property
        var workspace = await _context.Workspaces
            .Where(w => w.Id == channel.WorkspaceId)
            .Select(w => new { w.TenantId })
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace != null)
        {
            _context.Entry(channel).Property("TenantId").CurrentValue = workspace.TenantId;
        }

        await _context.Channels.AddAsync(channel, cancellationToken);
    }

    public Task UpdateAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _context.Channels.Update(channel);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .AnyAsync(c => c.Id == id && EF.Property<Guid>(c, "TenantId") == tenantId, cancellationToken);
    }
}