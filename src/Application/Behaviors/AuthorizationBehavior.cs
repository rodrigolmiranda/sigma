using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sigma.Application.Contracts;
using Sigma.Domain.Authorization;

namespace Sigma.Application.Behaviors;

public class AuthorizationBehavior : ICommandBehavior, IQueryBehavior
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> HandleAsync<TCommand>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var authResult = await AuthorizeAsync(command);
        if (!authResult.IsSuccess)
        {
            return authResult.Error!;
        }

        return await next(command, cancellationToken);
    }

    public async Task<Result<TResponse>> HandleAsync<TCommand, TResponse>(
        TCommand command,
        Func<TCommand, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var authResult = await AuthorizeAsync(command);
        if (!authResult.IsSuccess)
        {
            return authResult.Error!;
        }

        return await next(command, cancellationToken);
    }

    async Task<Result<TResponse>> IQueryBehavior.HandleAsync<TQuery, TResponse>(
        TQuery query,
        Func<TQuery, CancellationToken, Task<Result<TResponse>>> next,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(query);
        if (!authResult.IsSuccess)
        {
            return authResult.Error!;
        }

        return await next(query, cancellationToken);
    }

    private async Task<Result> AuthorizeAsync<T>(T request)
    {
        var authorizationHandler = _serviceProvider.GetService(typeof(IAuthorizationHandler<T>)) as IAuthorizationHandler<T>;

        if (authorizationHandler != null)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Check if user is null or not authenticated
            if (user == null)
            {
                return Error.Unauthorized("User is not authenticated");
            }

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return Error.Unauthorized("User is not authenticated");
            }

            try
            {
                var authResult = await authorizationHandler.AuthorizeAsync(request, user);
                if (!authResult)
                {
                    return Error.Forbidden("User is not authorized to perform this action");
                }
            }
            catch (Exception)
            {
                // If authorization handler throws, treat as unauthorized
                return Error.Unauthorized("Authorization failed");
            }
        }

        return Result.Success();
    }
}