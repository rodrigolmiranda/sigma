using Sigma.Application.Contracts;

namespace Sigma.Application.Behaviors;

public interface ICommandBehavior
{
    Task<Result> HandleAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<Result<TResponse>> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;
}