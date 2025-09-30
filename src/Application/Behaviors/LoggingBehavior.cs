using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Sigma.Application.Contracts;
using Sigma.Domain.Contracts;

namespace Sigma.Application.Behaviors;

public class LoggingBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly ILogger<LoggingBehavior> _logger;
    private readonly ICorrelationContext _correlationContext;

    public LoggingBehavior(
        ILogger<LoggingBehavior> logger,
        ICorrelationContext correlationContext)
    {
        _logger = logger;
        _correlationContext = correlationContext;
    }

    public async Task<Result> HandleAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CommandName"] = commandName,
            ["CorrelationId"] = _correlationContext.CorrelationId,
            ["UserId"] = _correlationContext.UserId,
            ["TenantId"] = _correlationContext.TenantId
        }))
        {
            _logger.LogInformation("Executing command {CommandName} with CorrelationId {CorrelationId}",
                commandName, _correlationContext.CorrelationId);
        }

        try
        {
            var result = await next(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Command {CommandName} executed successfully in {ElapsedMilliseconds}ms",
                    commandName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Command {CommandName} failed with error {ErrorCode} in {ElapsedMilliseconds}ms",
                    commandName, result.Error?.Code, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Command {CommandName} threw exception after {ElapsedMilliseconds}ms",
                commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Result<TResponse>> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Executing command {CommandName}", commandName);

        try
        {
            var result = await next(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Command {CommandName} executed successfully in {ElapsedMilliseconds}ms",
                    commandName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Command {CommandName} failed with error {ErrorCode} in {ElapsedMilliseconds}ms",
                    commandName, result.Error?.Code, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Command {CommandName} threw exception after {ElapsedMilliseconds}ms",
                commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    async Task<Result<TResponse>> IQueryBehavior.HandleAsync<TQuery, TResponse>(
        TQuery query,
        Func<TQuery, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken)
    {
        var queryName = typeof(TQuery).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Executing query {QueryName}", queryName);

        try
        {
            var result = await next(query, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation("Query {QueryName} executed successfully in {ElapsedMilliseconds}ms",
                    queryName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning("Query {QueryName} failed with error {ErrorCode} in {ElapsedMilliseconds}ms",
                    queryName, result.Error?.Code, stopwatch.ElapsedMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Query {QueryName} threw exception after {ElapsedMilliseconds}ms",
                queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}