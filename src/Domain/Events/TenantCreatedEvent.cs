using Sigma.Domain.Common;

namespace Sigma.Domain.Events;

public sealed record TenantCreatedEvent(
    Guid TenantId,
    string Name,
    string Slug,
    string PlanType,
    int RetentionDays,
    DateTime CreatedAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAtUtc => CreatedAtUtc;
}