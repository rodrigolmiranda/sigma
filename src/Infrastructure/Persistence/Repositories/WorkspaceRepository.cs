using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.Infrastructure.Persistence.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly SigmaDbContext _context;

    public WorkspaceRepository(SigmaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Include(w => w.Channels)
            .Where(w => w.TenantId == tenantId)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Workspace>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Where(w => w.TenantId == tenantId)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Workspace?> GetByExternalIdAsync(string externalId, Platform platform, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(externalId))
        {
            return null;
        }

        return await _context.Workspaces
            .Where(w => w.TenantId == tenantId)
            .Where(w => w.Platform == platform)
            .Where(w => w.ExternalId == externalId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        await _context.Workspaces.AddAsync(workspace, cancellationToken);
    }

    public Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _context.Workspaces.Update(workspace);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Where(w => w.TenantId == tenantId)
            .AnyAsync(w => w.Id == id, cancellationToken);
    }
}