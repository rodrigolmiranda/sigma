using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;

namespace Sigma.API.DataLoaders;

public class WorkspaceByTenantIdDataLoader : BatchDataLoader<Guid, IReadOnlyList<Workspace>>
{
    private readonly IDbContextFactory<SigmaDbContext> _dbContextFactory;

    public WorkspaceByTenantIdDataLoader(
        IDbContextFactory<SigmaDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Workspace>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var workspaces = await dbContext.Workspaces
            .Where(w => keys.Contains(w.TenantId))
            .ToListAsync(cancellationToken);

        return workspaces.GroupBy(w => w.TenantId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Workspace>)g.ToList());
    }
}