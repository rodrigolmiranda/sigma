namespace Sigma.Application.Common;

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }
    bool IsAuthenticated { get; }
    string? UserId { get; }
    IReadOnlyList<string> Roles { get; }
    bool HasRole(string role);
}