using Sigma.Domain.Common;

namespace Sigma.Domain.ValueObjects;

public class MessageSender : ValueObject
{
    public string PlatformUserId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsBot { get; private set; }

    private MessageSender()
    {
        PlatformUserId = string.Empty;
    }

    public MessageSender(string platformUserId, string? displayName, bool isBot)
    {
        PlatformUserId = platformUserId ?? throw new ArgumentNullException(nameof(platformUserId));
        DisplayName = displayName;
        IsBot = isBot;
    }

    public static MessageSender Unknown()
    {
        return new MessageSender("unknown", "Unknown User", false);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PlatformUserId;
        yield return DisplayName ?? string.Empty;
        yield return IsBot;
    }
}