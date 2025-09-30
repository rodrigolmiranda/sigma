namespace Sigma.Domain.Contracts;

public interface ITransactionManager
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    bool HasActiveTransaction { get; }
}