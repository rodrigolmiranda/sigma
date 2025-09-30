using Sigma.Domain.Contracts;

namespace Sigma.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string TenantSlug { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public string? UserId { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; }

    public TenantContext()
    {
        TenantId = Guid.Empty;
        TenantSlug = string.Empty;
        IsAuthenticated = false;
        Roles = new List<string>();
    }

    public TenantContext(Guid tenantId, string tenantSlug, bool isAuthenticated, string? userId, IEnumerable<string>? roles = null)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug ?? string.Empty;
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        Roles = roles?.ToList() ?? new List<string>();
    }

    public bool HasRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public static TenantContext Anonymous()
    {
        return new TenantContext();
    }

    public static TenantContext System()
    {
        return new TenantContext(
            Guid.Empty,
            "system",
            true,
            "system",
            new[] { "system", "admin" }
        );
    }
}