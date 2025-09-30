using Sigma.Application.Contracts;

namespace Sigma.Application.Behaviors;

public interface IQueryBehavior
{
    Task<Result<TResponse>> HandleAsync<TQuery, TResponse>(
        TQuery query,
        Func<TQuery, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}