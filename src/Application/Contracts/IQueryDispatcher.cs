namespace Sigma.Application.Contracts;

public interface IQueryDispatcher
{
    Task<Result<TResponse>> DispatchAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;
}