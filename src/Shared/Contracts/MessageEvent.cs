using Sigma.Shared.Enums;
using System.Text.Json;

namespace Sigma.Shared.Contracts;

public class MessageEvent
{
    public Guid Id { get; set; }
    public Platform Platform { get; set; }
    public string PlatformMessageId { get; set; } = string.Empty;
    public string? PlatformChannelId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid TenantId { get; set; }
    public MessageSenderInfo Sender { get; set; } = new();
    public MessageEventType Type { get; set; }
    public string? Text { get; set; }
    public List<MessageFragment> RichFragments { get; set; } = new();
    public List<MessageMedia> Media { get; set; } = new();
    public DateTime TimestampUtc { get; set; }
    public DateTime? EditedUtc { get; set; }
    public string? ReplyToPlatformMessageId { get; set; }
    public List<MessageReactionInfo> Reactions { get; set; } = new();
    public JsonDocument? Raw { get; set; }
    public int Version { get; set; } = 1;

    public MessageEvent()
    {
        Id = Guid.NewGuid();
        TimestampUtc = DateTime.UtcNow;
    }
}

public class MessageSenderInfo
{
    public string PlatformUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool IsBot { get; set; }
}

public enum MessageEventType
{
    Text,
    Image,
    File,
    Poll,
    System,
    Reaction,
    Unknown
}

public class MessageFragment
{
    public FragmentType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}

public enum FragmentType
{
    Link,
    Mention,
    Emoji,
    Hashtag,
    Code
}

public class MessageMedia
{
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long? Size { get; set; }
    public string? FileName { get; set; }
}

public class MessageReactionInfo
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}