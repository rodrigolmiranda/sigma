using Sigma.Domain.Common;

namespace Sigma.Domain.Events;

public sealed record WorkspaceCreatedEvent(
    Guid WorkspaceId,
    Guid TenantId,
    string Name,
    string Platform,
    DateTime CreatedAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAtUtc => CreatedAtUtc;
}