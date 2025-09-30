namespace Sigma.Domain.Contracts;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
    string? UserId { get; }
    string? TenantId { get; }
}