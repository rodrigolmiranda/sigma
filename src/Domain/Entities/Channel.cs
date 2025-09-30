using Sigma.Domain.Common;

namespace Sigma.Domain.Entities;

public class Channel : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string ExternalId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastMessageAtUtc { get; private set; }
    public int? RetentionOverrideDays { get; private set; }

    private readonly List<Message> _messages = new();
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    private Channel() : base()
    {
        Name = string.Empty;
        ExternalId = string.Empty;
    }

    public Channel(Guid workspaceId, string name, string externalId) : base()
    {
        WorkspaceId = workspaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ExternalId = externalId ?? throw new ArgumentNullException(nameof(externalId));
        IsActive = true;
    }

    public void UpdateLastMessageTime(DateTime messageTime)
    {
        LastMessageAtUtc = messageTime;
    }

    public void SetRetentionOverride(int? days)
    {
        if (days.HasValue && days.Value <= 0)
            throw new ArgumentException("Retention override days must be positive", nameof(days));

        RetentionOverrideDays = days;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}