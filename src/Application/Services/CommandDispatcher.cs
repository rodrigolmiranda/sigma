using Microsoft.Extensions.DependencyInjection;
using Sigma.Application.Behaviors;
using Sigma.Application.Contracts;

namespace Sigma.Application.Services;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ICommandBehavior> _behaviors;

    public CommandDispatcher(IServiceProvider serviceProvider, IEnumerable<ICommandBehavior> behaviors)
    {
        _serviceProvider = serviceProvider;
        _behaviors = behaviors;
    }

    public async Task<Result> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();

        Func<TCommand, CancellationToken, Task<Result>> pipeline = handler.HandleAsync;

        foreach (var behavior in _behaviors.Reverse())
        {
            var currentBehavior = behavior;
            var next = pipeline;
            pipeline = (cmd, ct) => currentBehavior.HandleAsync(cmd, next, ct);
        }

        return await pipeline(command, cancellationToken);
    }

    public async Task<Result<TResponse>> DispatchAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        Func<TCommand, CancellationToken, Task<Result<TResponse>>> pipeline = handler.HandleAsync;

        foreach (var behavior in _behaviors.Reverse())
        {
            var currentBehavior = behavior;
            var next = pipeline;
            pipeline = (cmd, ct) => currentBehavior.HandleAsync(cmd, next, ct);
        }

        return await pipeline(command, cancellationToken);
    }
}