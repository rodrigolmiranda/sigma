namespace Sigma.Domain.Contracts;

public interface ITenantContext
{
    Guid TenantId { get; }
    string? TenantSlug { get; }
}