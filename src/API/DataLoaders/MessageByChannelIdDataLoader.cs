using HotChocolate;
using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;

namespace Sigma.API.DataLoaders;

public class MessageByChannelIdDataLoader : BatchDataLoader<Guid, IReadOnlyList<Message>>
{
    private readonly IDbContextFactory<SigmaDbContext> _dbContextFactory;

    public MessageByChannelIdDataLoader(
        IDbContextFactory<SigmaDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Message>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var messages = await dbContext.Messages
            .Where(m => keys.Contains(m.ChannelId))
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return messages.GroupBy(m => m.ChannelId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Message>)g.ToList());
    }
}