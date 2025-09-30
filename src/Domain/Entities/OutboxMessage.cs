using Sigma.Domain.Common;

namespace Sigma.Domain.Entities;

public class OutboxMessage : Entity
{
    public string EventType { get; private set; } = string.Empty;
    public string EventData { get; private set; } = string.Empty;
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }

    private OutboxMessage() { } // EF Core

    public OutboxMessage(string eventType, string eventData)
        : base()
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        Error = null;
        NextRetryAtUtc = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;

        // Exponential backoff: 1min, 2min, 4min, 8min, 16min, 32min, then stop
        if (RetryCount <= 6)
        {
            var delayMinutes = Math.Pow(2, RetryCount - 1);
            NextRetryAtUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            NextRetryAtUtc = null; // Give up after 6 retries
        }
    }

    public bool CanRetry()
    {
        return ProcessedAtUtc == null
            && RetryCount < 6
            && (NextRetryAtUtc == null || NextRetryAtUtc <= DateTime.UtcNow);
    }
}