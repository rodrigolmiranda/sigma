using Sigma.Domain.Common;

namespace Sigma.Domain.Events;

public sealed record MessageReceivedEvent(
    Guid MessageId,
    Guid ChannelId,
    Guid WorkspaceId,
    Guid TenantId,
    string Content,
    string AuthorName,
    DateTime MessageTimestamp,
    DateTime ReceivedAtUtc) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAtUtc => ReceivedAtUtc;
}