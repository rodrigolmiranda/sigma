namespace Sigma.Shared.Contracts;

public abstract class WebhookPayload
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string Platform { get; set; } = string.Empty;
    public string? Signature { get; set; }
}

public class SlackWebhookPayload : WebhookPayload
{
    public string? Token { get; set; }
    public string? TeamId { get; set; }
    public string? ApiAppId { get; set; }
    public string? Event { get; set; }
    public string? EventTime { get; set; }
    public string? SlackEventId { get; set; }
    public string? Type { get; set; }
    public string? Challenge { get; set; }
}

public class DiscordWebhookPayload : WebhookPayload
{
    public string? GuildId { get; set; }
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
    public string? Content { get; set; }
    public string? AuthorId { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class TelegramWebhookPayload : WebhookPayload
{
    public long UpdateId { get; set; }
    public object? Message { get; set; }
    public object? EditedMessage { get; set; }
    public object? ChannelPost { get; set; }
    public object? EditedChannelPost { get; set; }
}

public class WhatsAppWebhookPayload : WebhookPayload
{
    public string? Object { get; set; }
    public List<WhatsAppEntry>? Entry { get; set; }
}

public class WhatsAppEntry
{
    public string? Id { get; set; }
    public List<WhatsAppChange>? Changes { get; set; }
}

public class WhatsAppChange
{
    public string? Field { get; set; }
    public object? Value { get; set; }
}