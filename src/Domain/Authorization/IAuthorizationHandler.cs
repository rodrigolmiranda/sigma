using System.Security.Claims;

namespace Sigma.Domain.Authorization;

public interface IAuthorizationHandler<in TRequest>
{
    Task<bool> AuthorizeAsync(TRequest request, ClaimsPrincipal user);
}