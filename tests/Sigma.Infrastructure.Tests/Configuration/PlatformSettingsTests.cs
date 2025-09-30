using System.Collections.Generic;
using Sigma.Shared.Configuration;
using Xunit;

namespace Sigma.Infrastructure.Tests.Configuration;

public class PlatformSettingsTests
{
    [Fact]
    public void PlatformSettings_DefaultConstructor_InitializesAllProperties()
    {
        // Act
        var settings = new PlatformSettings();

        // Assert
        Assert.NotNull(settings.Slack);
        Assert.NotNull(settings.Discord);
        Assert.NotNull(settings.Telegram);
        Assert.NotNull(settings.WhatsApp);
    }

    [Fact]
    public void PlatformSettings_CanSetAndGetAllProperties()
    {
        // Arrange
        var settings = new PlatformSettings();
        var slackSettings = new SlackSettings { SigningSecret = "slack-secret" };
        var discordSettings = new DiscordSettings { BotToken = "discord-token" };
        var telegramSettings = new TelegramSettings();
        var whatsAppSettings = new WhatsAppSettings { AppSecret = "whatsapp-secret" };

        // Act
        settings.Slack = slackSettings;
        settings.Discord = discordSettings;
        settings.Telegram = telegramSettings;
        settings.WhatsApp = whatsAppSettings;

        // Assert
        Assert.Same(slackSettings, settings.Slack);
        Assert.Same(discordSettings, settings.Discord);
        Assert.Same(telegramSettings, settings.Telegram);
        Assert.Same(whatsAppSettings, settings.WhatsApp);
    }

    [Fact]
    public void SlackSettings_DefaultConstructor_InitializesWithNullValues()
    {
        // Act
        var settings = new SlackSettings();

        // Assert
        Assert.Null(settings.SigningSecret);
        Assert.Null(settings.ClientId);
        Assert.Null(settings.ClientSecret);
    }

    [Fact]
    public void SlackSettings_CanSetAndGetAllProperties()
    {
        // Arrange
        var settings = new SlackSettings();
        var signingSecret = "test-signing-secret";
        var clientId = "test-client-id";
        var clientSecret = "test-client-secret";

        // Act
        settings.SigningSecret = signingSecret;
        settings.ClientId = clientId;
        settings.ClientSecret = clientSecret;

        // Assert
        Assert.Equal(signingSecret, settings.SigningSecret);
        Assert.Equal(clientId, settings.ClientId);
        Assert.Equal(clientSecret, settings.ClientSecret);
    }

    [Fact]
    public void DiscordSettings_DefaultConstructor_InitializesWithNullValues()
    {
        // Act
        var settings = new DiscordSettings();

        // Assert
        Assert.Null(settings.BotToken);
        Assert.Null(settings.ApplicationId);
    }

    [Fact]
    public void DiscordSettings_CanSetAndGetAllProperties()
    {
        // Arrange
        var settings = new DiscordSettings();
        var botToken = "test-bot-token";
        var applicationId = "test-app-id";

        // Act
        settings.BotToken = botToken;
        settings.ApplicationId = applicationId;

        // Assert
        Assert.Equal(botToken, settings.BotToken);
        Assert.Equal(applicationId, settings.ApplicationId);
    }

    [Fact]
    public void TelegramSettings_DefaultConstructor_InitializesEmptyDictionary()
    {
        // Act
        var settings = new TelegramSettings();

        // Assert
        Assert.NotNull(settings.BotTokens);
        Assert.Empty(settings.BotTokens);
    }

    [Fact]
    public void TelegramSettings_CanAddAndRetrieveBotTokens()
    {
        // Arrange
        var settings = new TelegramSettings();

        // Act
        settings.BotTokens["bot1"] = "token1";
        settings.BotTokens["bot2"] = "token2";
        settings.BotTokens.Add("bot3", "token3");

        // Assert
        Assert.Equal(3, settings.BotTokens.Count);
        Assert.Equal("token1", settings.BotTokens["bot1"]);
        Assert.Equal("token2", settings.BotTokens["bot2"]);
        Assert.Equal("token3", settings.BotTokens["bot3"]);
    }

    [Fact]
    public void TelegramSettings_CanSetNewDictionary()
    {
        // Arrange
        var settings = new TelegramSettings();
        var newTokens = new Dictionary<string, string>
        {
            { "bot1", "token1" },
            { "bot2", "token2" }
        };

        // Act
        settings.BotTokens = newTokens;

        // Assert
        Assert.Same(newTokens, settings.BotTokens);
        Assert.Equal(2, settings.BotTokens.Count);
    }

    [Fact]
    public void TelegramSettings_CanClearBotTokens()
    {
        // Arrange
        var settings = new TelegramSettings();
        settings.BotTokens["bot1"] = "token1";
        settings.BotTokens["bot2"] = "token2";

        // Act
        settings.BotTokens.Clear();

        // Assert
        Assert.Empty(settings.BotTokens);
    }

    [Fact]
    public void WhatsAppSettings_DefaultConstructor_InitializesWithNullValues()
    {
        // Act
        var settings = new WhatsAppSettings();

        // Assert
        Assert.Null(settings.AppSecret);
        Assert.Null(settings.BusinessAccountId);
        Assert.Null(settings.AccessToken);
    }

    [Fact]
    public void WhatsAppSettings_CanSetAndGetAllProperties()
    {
        // Arrange
        var settings = new WhatsAppSettings();
        var appSecret = "test-app-secret";
        var businessAccountId = "test-business-account";
        var accessToken = "test-access-token";

        // Act
        settings.AppSecret = appSecret;
        settings.BusinessAccountId = businessAccountId;
        settings.AccessToken = accessToken;

        // Assert
        Assert.Equal(appSecret, settings.AppSecret);
        Assert.Equal(businessAccountId, settings.BusinessAccountId);
        Assert.Equal(accessToken, settings.AccessToken);
    }

    [Fact]
    public void PlatformSettings_CanBeNested()
    {
        // Arrange
        var settings = new PlatformSettings
        {
            Slack = new SlackSettings
            {
                SigningSecret = "slack-secret",
                ClientId = "slack-client",
                ClientSecret = "slack-client-secret"
            },
            Discord = new DiscordSettings
            {
                BotToken = "discord-token",
                ApplicationId = "discord-app"
            },
            Telegram = new TelegramSettings
            {
                BotTokens = new Dictionary<string, string>
                {
                    { "bot1", "token1" },
                    { "bot2", "token2" }
                }
            },
            WhatsApp = new WhatsAppSettings
            {
                AppSecret = "whatsapp-secret",
                BusinessAccountId = "whatsapp-business",
                AccessToken = "whatsapp-token"
            }
        };

        // Assert
        Assert.Equal("slack-secret", settings.Slack.SigningSecret);
        Assert.Equal("discord-token", settings.Discord.BotToken);
        Assert.Equal(2, settings.Telegram.BotTokens.Count);
        Assert.Equal("whatsapp-secret", settings.WhatsApp.AppSecret);
    }

    [Fact]
    public void AllSettings_CanHandleNullAndEmptyStrings()
    {
        // Arrange & Act
        var slackSettings = new SlackSettings
        {
            SigningSecret = null,
            ClientId = "",
            ClientSecret = "   "
        };

        var discordSettings = new DiscordSettings
        {
            BotToken = null,
            ApplicationId = ""
        };

        var whatsAppSettings = new WhatsAppSettings
        {
            AppSecret = null,
            BusinessAccountId = "",
            AccessToken = "   "
        };

        // Assert
        Assert.Null(slackSettings.SigningSecret);
        Assert.Equal("", slackSettings.ClientId);
        Assert.Equal("   ", slackSettings.ClientSecret);

        Assert.Null(discordSettings.BotToken);
        Assert.Equal("", discordSettings.ApplicationId);

        Assert.Null(whatsAppSettings.AppSecret);
        Assert.Equal("", whatsAppSettings.BusinessAccountId);
        Assert.Equal("   ", whatsAppSettings.AccessToken);
    }

    [Fact]
    public void TelegramSettings_BotTokens_CanBeReplaced()
    {
        // Arrange
        var settings = new TelegramSettings();
        settings.BotTokens["original"] = "original-token";

        // Act
        settings.BotTokens = new Dictionary<string, string>
        {
            { "new", "new-token" }
        };

        // Assert
        Assert.Single(settings.BotTokens);
        Assert.False(settings.BotTokens.ContainsKey("original"));
        Assert.True(settings.BotTokens.ContainsKey("new"));
        Assert.Equal("new-token", settings.BotTokens["new"]);
    }

    [Fact]
    public void PlatformSettings_PropertiesAreIndependent()
    {
        // Arrange
        var settings1 = new PlatformSettings();
        var settings2 = new PlatformSettings();

        // Act
        settings1.Slack.SigningSecret = "secret1";
        settings2.Slack.SigningSecret = "secret2";

        // Assert
        Assert.Equal("secret1", settings1.Slack.SigningSecret);
        Assert.Equal("secret2", settings2.Slack.SigningSecret);
        Assert.NotSame(settings1.Slack, settings2.Slack);
    }
}
