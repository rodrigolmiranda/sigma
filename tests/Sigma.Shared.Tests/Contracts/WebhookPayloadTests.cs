using System.Text.Json;
using Sigma.Shared.Contracts;
using Xunit;

namespace Sigma.Shared.Tests.Contracts;

public class WebhookPayloadTests
{
    [Fact]
    public void WebhookPayload_ShouldInitializeWithDefaults()
    {
        // Act
        var payload = new TestWebhookPayload();

        // Assert
        Assert.NotNull(payload.EventId);
        Assert.NotEmpty(payload.EventId);
        Assert.True(Guid.TryParse(payload.EventId, out _));
        Assert.True((DateTime.UtcNow - payload.ReceivedAtUtc).TotalSeconds < 1);
        Assert.Equal(string.Empty, payload.Platform);
        Assert.Null(payload.Signature);
    }

    [Fact]
    public void WebhookPayload_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var payload = new TestWebhookPayload();
        var eventId = Guid.NewGuid().ToString();
        var receivedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        payload.EventId = eventId;
        payload.ReceivedAtUtc = receivedAt;
        payload.Platform = "TestPlatform";
        payload.Signature = "signature123";

        // Assert
        Assert.Equal(eventId, payload.EventId);
        Assert.Equal(receivedAt, payload.ReceivedAtUtc);
        Assert.Equal("TestPlatform", payload.Platform);
        Assert.Equal("signature123", payload.Signature);
    }

    private class TestWebhookPayload : WebhookPayload { }
}

public class SlackWebhookPayloadTests
{
    [Fact]
    public void SlackWebhookPayload_ShouldInitializeWithDefaults()
    {
        // Act
        var payload = new SlackWebhookPayload();

        // Assert
        Assert.NotNull(payload.EventId);
        Assert.True((DateTime.UtcNow - payload.ReceivedAtUtc).TotalSeconds < 1);
        Assert.Equal(string.Empty, payload.Platform);
        Assert.Null(payload.Token);
        Assert.Null(payload.TeamId);
        Assert.Null(payload.ApiAppId);
        Assert.Null(payload.Event);
        Assert.Null(payload.EventTime);
        Assert.Null(payload.SlackEventId);
        Assert.Null(payload.Type);
        Assert.Null(payload.Challenge);
    }

    [Fact]
    public void SlackWebhookPayload_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var payload = new SlackWebhookPayload();

        // Act
        payload.Platform = "Slack";
        payload.Token = "token123";
        payload.TeamId = "T123456";
        payload.ApiAppId = "A123456";
        payload.Event = "message";
        payload.EventTime = "1234567890";
        payload.SlackEventId = "Ev123456";
        payload.Type = "event_callback";
        payload.Challenge = "challenge123";

        // Assert
        Assert.Equal("Slack", payload.Platform);
        Assert.Equal("token123", payload.Token);
        Assert.Equal("T123456", payload.TeamId);
        Assert.Equal("A123456", payload.ApiAppId);
        Assert.Equal("message", payload.Event);
        Assert.Equal("1234567890", payload.EventTime);
        Assert.Equal("Ev123456", payload.SlackEventId);
        Assert.Equal("event_callback", payload.Type);
        Assert.Equal("challenge123", payload.Challenge);
    }

    [Fact]
    public void SlackWebhookPayload_UrlVerification_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var payload = new SlackWebhookPayload
        {
            Type = "url_verification",
            Challenge = "3eZbrw1aBm2rZgRNFdxV2595E9CY3gmdALWMmHkvFXO7tYXAYM8P",
            Token = "Jhj5dZrVaK7ZwHHjRyZWjbDl"
        };

        // Assert
        Assert.Equal("url_verification", payload.Type);
        Assert.NotNull(payload.Challenge);
        Assert.NotEmpty(payload.Challenge);
        Assert.NotNull(payload.Token);
    }

    [Fact]
    public void SlackWebhookPayload_EventCallback_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var payload = new SlackWebhookPayload
        {
            Type = "event_callback",
            Token = "token123",
            TeamId = "T123456",
            ApiAppId = "A123456",
            Event = "message",
            EventTime = "1234567890",
            SlackEventId = "Ev123456"
        };

        // Assert
        Assert.Equal("event_callback", payload.Type);
        Assert.Null(payload.Challenge);
        Assert.Equal("message", payload.Event);
        Assert.NotNull(payload.SlackEventId);
    }
}

public class DiscordWebhookPayloadTests
{
    [Fact]
    public void DiscordWebhookPayload_ShouldInitializeWithDefaults()
    {
        // Act
        var payload = new DiscordWebhookPayload();

        // Assert
        Assert.NotNull(payload.EventId);
        Assert.True((DateTime.UtcNow - payload.ReceivedAtUtc).TotalSeconds < 1);
        Assert.Equal(string.Empty, payload.Platform);
        Assert.Null(payload.GuildId);
        Assert.Null(payload.ChannelId);
        Assert.Null(payload.MessageId);
        Assert.Null(payload.Content);
        Assert.Null(payload.AuthorId);
        Assert.Null(payload.Timestamp);
    }

    [Fact]
    public void DiscordWebhookPayload_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var payload = new DiscordWebhookPayload();
        var timestamp = DateTime.UtcNow.AddHours(-1);

        // Act
        payload.Platform = "Discord";
        payload.GuildId = "guild123";
        payload.ChannelId = "channel123";
        payload.MessageId = "msg123";
        payload.Content = "Hello, Discord!";
        payload.AuthorId = "author123";
        payload.Timestamp = timestamp;

        // Assert
        Assert.Equal("Discord", payload.Platform);
        Assert.Equal("guild123", payload.GuildId);
        Assert.Equal("channel123", payload.ChannelId);
        Assert.Equal("msg123", payload.MessageId);
        Assert.Equal("Hello, Discord!", payload.Content);
        Assert.Equal("author123", payload.AuthorId);
        Assert.Equal(timestamp, payload.Timestamp);
    }
}

public class TelegramWebhookPayloadTests
{
    [Fact]
    public void TelegramWebhookPayload_ShouldInitializeWithDefaults()
    {
        // Act
        var payload = new TelegramWebhookPayload();

        // Assert
        Assert.NotNull(payload.EventId);
        Assert.True((DateTime.UtcNow - payload.ReceivedAtUtc).TotalSeconds < 1);
        Assert.Equal(string.Empty, payload.Platform);
        Assert.Equal(0, payload.UpdateId);
        Assert.Null(payload.Message);
        Assert.Null(payload.EditedMessage);
        Assert.Null(payload.ChannelPost);
        Assert.Null(payload.EditedChannelPost);
    }

    [Fact]
    public void TelegramWebhookPayload_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var payload = new TelegramWebhookPayload();
        var messageObj = new { text = "Hello", chat = new { id = 123 } };
        var editedMessageObj = new { text = "Edited", chat = new { id = 456 } };

        // Act
        payload.Platform = "Telegram";
        payload.UpdateId = 987654321;
        payload.Message = messageObj;
        payload.EditedMessage = editedMessageObj;
        payload.ChannelPost = new { text = "Channel post" };
        payload.EditedChannelPost = new { text = "Edited channel post" };

        // Assert
        Assert.Equal("Telegram", payload.Platform);
        Assert.Equal(987654321, payload.UpdateId);
        Assert.NotNull(payload.Message);
        Assert.NotNull(payload.EditedMessage);
        Assert.NotNull(payload.ChannelPost);
        Assert.NotNull(payload.EditedChannelPost);
    }

    [Theory]
    [InlineData(1234567890)]
    [InlineData(9876543210)]
    [InlineData(0)]
    [InlineData(-1)]
    public void TelegramWebhookPayload_UpdateId_ShouldHandleVariousValues(long updateId)
    {
        // Arrange & Act
        var payload = new TelegramWebhookPayload { UpdateId = updateId };

        // Assert
        Assert.Equal(updateId, payload.UpdateId);
    }
}

public class WhatsAppWebhookPayloadTests
{
    [Fact]
    public void WhatsAppWebhookPayload_ShouldInitializeWithDefaults()
    {
        // Act
        var payload = new WhatsAppWebhookPayload();

        // Assert
        Assert.NotNull(payload.EventId);
        Assert.True((DateTime.UtcNow - payload.ReceivedAtUtc).TotalSeconds < 1);
        Assert.Equal(string.Empty, payload.Platform);
        Assert.Null(payload.Object);
        Assert.Null(payload.Entry);
    }

    [Fact]
    public void WhatsAppWebhookPayload_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var payload = new WhatsAppWebhookPayload();
        var entries = new List<WhatsAppEntry>
        {
            new WhatsAppEntry
            {
                Id = "entry1",
                Changes = new List<WhatsAppChange>
                {
                    new WhatsAppChange { Field = "messages", Value = new { text = "Hello" } }
                }
            }
        };

        // Act
        payload.Platform = "WhatsApp";
        payload.Object = "whatsapp_business_account";
        payload.Entry = entries;

        // Assert
        Assert.Equal("WhatsApp", payload.Platform);
        Assert.Equal("whatsapp_business_account", payload.Object);
        Assert.NotNull(payload.Entry);
        Assert.Single(payload.Entry);
        Assert.Equal("entry1", payload.Entry[0].Id);
    }
}

public class WhatsAppEntryTests
{
    [Fact]
    public void WhatsAppEntry_ShouldInitializeWithDefaults()
    {
        // Act
        var entry = new WhatsAppEntry();

        // Assert
        Assert.Null(entry.Id);
        Assert.Null(entry.Changes);
    }

    [Fact]
    public void WhatsAppEntry_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var entry = new WhatsAppEntry();
        var changes = new List<WhatsAppChange>
        {
            new WhatsAppChange { Field = "messages", Value = "test" },
            new WhatsAppChange { Field = "statuses", Value = new { status = "delivered" } }
        };

        // Act
        entry.Id = "entry123";
        entry.Changes = changes;

        // Assert
        Assert.Equal("entry123", entry.Id);
        Assert.NotNull(entry.Changes);
        Assert.Equal(2, entry.Changes.Count);
        Assert.Equal("messages", entry.Changes[0].Field);
        Assert.Equal("statuses", entry.Changes[1].Field);
    }
}

public class WhatsAppChangeTests
{
    [Fact]
    public void WhatsAppChange_ShouldInitializeWithDefaults()
    {
        // Act
        var change = new WhatsAppChange();

        // Assert
        Assert.Null(change.Field);
        Assert.Null(change.Value);
    }

    [Fact]
    public void WhatsAppChange_Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var change = new WhatsAppChange();
        var complexValue = new
        {
            messages = new[]
            {
                new { id = "msg1", text = "Hello" },
                new { id = "msg2", text = "World" }
            }
        };

        // Act
        change.Field = "messages";
        change.Value = complexValue;

        // Assert
        Assert.Equal("messages", change.Field);
        Assert.NotNull(change.Value);
    }

    [Theory]
    [InlineData("messages")]
    [InlineData("statuses")]
    [InlineData("contacts")]
    [InlineData("errors")]
    public void WhatsAppChange_Field_ShouldHandleVariousValues(string field)
    {
        // Arrange & Act
        var change = new WhatsAppChange { Field = field, Value = new { test = "data" } };

        // Assert
        Assert.Equal(field, change.Field);
        Assert.NotNull(change.Value);
    }

    [Fact]
    public void WhatsAppChange_Value_ShouldHandleComplexObjects()
    {
        // Arrange
        var change = new WhatsAppChange();
        var jsonString = """{"messages":[{"id":"123","from":"456","text":{"body":"Hello"}}]}""";
        var jsonValue = JsonDocument.Parse(jsonString).RootElement;

        // Act
        change.Field = "messages";
        change.Value = jsonValue;

        // Assert
        Assert.Equal("messages", change.Field);
        Assert.NotNull(change.Value);
    }
}