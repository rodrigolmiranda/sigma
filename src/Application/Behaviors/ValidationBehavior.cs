using FluentValidation;
using Sigma.Application.Contracts;

namespace Sigma.Application.Behaviors;

public class ValidationBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> HandleAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<TCommand>)) as IValidator<TCommand>;

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                return Error.Validation(firstError?.ErrorMessage ?? "Validation failed");
            }
        }

        return await next(command, cancellationToken);
    }

    public async Task<Result<TResponse>> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<TCommand>)) as IValidator<TCommand>;

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                return Error.Validation(firstError?.ErrorMessage ?? "Validation failed");
            }
        }

        return await next(command, cancellationToken);
    }

    async Task<Result<TResponse>> IQueryBehavior.HandleAsync<TQuery, TResponse>(
        TQuery query,
        Func<TQuery, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<TQuery>)) as IValidator<TQuery>;

        if (validator != null)
        {
            var validationResult = await validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                return Error.Validation(firstError?.ErrorMessage ?? "Validation failed");
            }
        }

        return await next(query, cancellationToken);
    }
}