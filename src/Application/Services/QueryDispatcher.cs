using Microsoft.Extensions.DependencyInjection;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;

namespace Sigma.Application.Services;

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IQueryBehavior> _behaviors;

    public QueryDispatcher(IServiceProvider serviceProvider, IEnumerable<IQueryBehavior> behaviors)
    {
        _serviceProvider = serviceProvider;
        _behaviors = behaviors;
    }

    public async Task<Result<TResponse>> DispatchAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        Func<TQuery, CancellationToken, Task<Result<TResponse>>> pipeline = handler.HandleAsync;

        foreach (var behavior in _behaviors.Reverse())
        {
            var currentBehavior = behavior;
            var next = pipeline;
            pipeline = (qry, ct) => currentBehavior.HandleAsync(qry, next, ct);
        }

        return await pipeline(query, cancellationToken);
    }
}