namespace Sigma.Shared.Configuration;

public class PlatformSettings
{
    public SlackSettings Slack { get; set; } = new();
    public DiscordSettings Discord { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
    public WhatsAppSettings WhatsApp { get; set; } = new();
}

public class SlackSettings
{
    public string? SigningSecret { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class DiscordSettings
{
    public string? BotToken { get; set; }
    public string? ApplicationId { get; set; }
}

public class TelegramSettings
{
    public Dictionary<string, string> BotTokens { get; set; } = new();
}

public class WhatsAppSettings
{
    public string? AppSecret { get; set; }
    public string? BusinessAccountId { get; set; }
    public string? AccessToken { get; set; }
}