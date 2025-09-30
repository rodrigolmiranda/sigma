using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;

namespace Sigma.Application.Behaviors;

public class TransactionBehavior : ICommandBehavior
{
    private readonly ITransactionManager _transactionManager;

    public TransactionBehavior(ITransactionManager transactionManager)
    {
        _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
    }

    public async Task<Result> HandleAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        return await _transactionManager.ExecuteInTransactionAsync(
            async ct => await next(command, ct),
            cancellationToken);
    }

    public async Task<Result<TResponse>> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        return await _transactionManager.ExecuteInTransactionAsync(
            async ct => await next(command, ct),
            cancellationToken);
    }
}