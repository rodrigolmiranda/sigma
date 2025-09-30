using Sigma.Domain.Common;

namespace Sigma.Domain.Entities;

public class WebhookEvent : Entity
{
    public string Platform { get; private set; } = string.Empty;
    public string EventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ProcessingError { get; private set; }

    private WebhookEvent() { } // EF Core

    public WebhookEvent(
        string platform,
        string eventId,
        string eventType,
        string payload)
        : base()
    {
        Platform = platform ?? throw new ArgumentNullException(nameof(platform));
        EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        ReceivedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        ProcessingError = null;
    }

    public void MarkAsFailed(string error)
    {
        ProcessedAtUtc = DateTime.UtcNow;
        ProcessingError = error;
    }

    public bool WasProcessed => ProcessedAtUtc.HasValue && ProcessingError == null;
}