using Sigma.Domain.Common;
using Sigma.Domain.ValueObjects;

namespace Sigma.Domain.Entities;

public class Message : Entity
{
    public Guid ChannelId { get; private set; }
    public Guid TenantId { get; private set; }
    public string PlatformMessageId { get; private set; }
    public MessageSender Sender { get; private set; }
    public MessageType Type { get; private set; }
    public string? Text { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }
    public string? ReplyToPlatformMessageId { get; private set; }
    public bool IsDeleted { get; private set; }

    private readonly List<MessageReaction> _reactions = new();
    public IReadOnlyList<MessageReaction> Reactions => _reactions.AsReadOnly();

    private Message() : base()
    {
        PlatformMessageId = string.Empty;
        Sender = null!; // EF Core will initialize this
    }

    public Message(
        Guid channelId,
        Guid tenantId,
        string platformMessageId,
        MessageSender sender,
        MessageType type,
        string? text,
        DateTime timestampUtc) : base()
    {
        ChannelId = channelId;
        TenantId = tenantId;
        PlatformMessageId = platformMessageId ?? throw new ArgumentNullException(nameof(platformMessageId));
        Sender = sender ?? throw new ArgumentNullException(nameof(sender));
        Type = type;
        Text = text;
        TimestampUtc = timestampUtc;
        IsDeleted = false;
    }

    public void MarkAsEdited(string newText, DateTime editedAt)
    {
        Text = newText;
        EditedAtUtc = editedAt;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    public void AddReaction(string key, int count)
    {
        var existing = _reactions.FirstOrDefault(r => r.Key == key);
        if (existing != null)
        {
            _reactions.Remove(existing);
        }
        _reactions.Add(new MessageReaction(key, count));
    }
}