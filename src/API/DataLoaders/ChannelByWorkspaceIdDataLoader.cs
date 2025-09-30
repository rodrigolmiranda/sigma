using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;

namespace Sigma.API.DataLoaders;

public class ChannelByWorkspaceIdDataLoader : BatchDataLoader<Guid, IReadOnlyList<Channel>>
{
    private readonly IDbContextFactory<SigmaDbContext> _dbContextFactory;

    public ChannelByWorkspaceIdDataLoader(
        IDbContextFactory<SigmaDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Channel>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var channels = await dbContext.Channels
            .Where(c => keys.Contains(c.WorkspaceId))
            .ToListAsync(cancellationToken);

        return channels.GroupBy(c => c.WorkspaceId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Channel>)g.ToList());
    }
}