using System.Text.Json;
using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Shared.Tests.Contracts;

public class MessageEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var messageEvent = new MessageEvent();

        // Assert
        Assert.NotEqual(Guid.Empty, messageEvent.Id);
        Assert.Equal(string.Empty, messageEvent.PlatformMessageId);
        Assert.NotNull(messageEvent.Sender);
        Assert.NotNull(messageEvent.RichFragments);
        Assert.Empty(messageEvent.RichFragments);
        Assert.NotNull(messageEvent.Media);
        Assert.Empty(messageEvent.Media);
        Assert.NotNull(messageEvent.Reactions);
        Assert.Empty(messageEvent.Reactions);
        Assert.Equal(1, messageEvent.Version);
        Assert.True((DateTime.UtcNow - messageEvent.TimestampUtc).TotalSeconds < 1);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var messageEvent = new MessageEvent();
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        messageEvent.Platform = Platform.Slack;
        messageEvent.PlatformMessageId = "msg123";
        messageEvent.PlatformChannelId = "channel123";
        messageEvent.WorkspaceId = workspaceId;
        messageEvent.TenantId = tenantId;
        messageEvent.Type = MessageEventType.Text;
        messageEvent.Text = "Test message";
        messageEvent.TimestampUtc = now;
        messageEvent.EditedUtc = now.AddMinutes(5);
        messageEvent.ReplyToPlatformMessageId = "reply123";
        messageEvent.Version = 2;

        // Assert
        Assert.Equal(Platform.Slack, messageEvent.Platform);
        Assert.Equal("msg123", messageEvent.PlatformMessageId);
        Assert.Equal("channel123", messageEvent.PlatformChannelId);
        Assert.Equal(workspaceId, messageEvent.WorkspaceId);
        Assert.Equal(tenantId, messageEvent.TenantId);
        Assert.Equal(MessageEventType.Text, messageEvent.Type);
        Assert.Equal("Test message", messageEvent.Text);
        Assert.Equal(now, messageEvent.TimestampUtc);
        Assert.Equal(now.AddMinutes(5), messageEvent.EditedUtc);
        Assert.Equal("reply123", messageEvent.ReplyToPlatformMessageId);
        Assert.Equal(2, messageEvent.Version);
    }

    [Fact]
    public void Sender_ShouldHandleProperties()
    {
        // Arrange
        var messageEvent = new MessageEvent();

        // Act
        messageEvent.Sender = new MessageSenderInfo
        {
            PlatformUserId = "user123",
            DisplayName = "John Doe",
            IsBot = true
        };

        // Assert
        Assert.Equal("user123", messageEvent.Sender.PlatformUserId);
        Assert.Equal("John Doe", messageEvent.Sender.DisplayName);
        Assert.True(messageEvent.Sender.IsBot);
    }

    [Fact]
    public void RichFragments_ShouldHandleCollection()
    {
        // Arrange
        var messageEvent = new MessageEvent();
        var fragments = new List<MessageFragment>
        {
            new MessageFragment
            {
                Type = FragmentType.Link,
                Value = "https://example.com",
                Url = "https://example.com",
                StartIndex = 0,
                EndIndex = 19
            },
            new MessageFragment
            {
                Type = FragmentType.Mention,
                Value = "@user",
                StartIndex = 20,
                EndIndex = 25
            }
        };

        // Act
        messageEvent.RichFragments = fragments;

        // Assert
        Assert.Equal(2, messageEvent.RichFragments.Count);
        Assert.Equal(FragmentType.Link, messageEvent.RichFragments[0].Type);
        Assert.Equal("https://example.com", messageEvent.RichFragments[0].Value);
        Assert.Equal(FragmentType.Mention, messageEvent.RichFragments[1].Type);
        Assert.Equal("@user", messageEvent.RichFragments[1].Value);
    }

    [Fact]
    public void Media_ShouldHandleCollection()
    {
        // Arrange
        var messageEvent = new MessageEvent();
        var media = new List<MessageMedia>
        {
            new MessageMedia
            {
                Url = "https://example.com/image.jpg",
                MimeType = "image/jpeg",
                Size = 1024,
                FileName = "image.jpg"
            },
            new MessageMedia
            {
                Url = "https://example.com/doc.pdf",
                MimeType = "application/pdf",
                Size = 2048,
                FileName = "doc.pdf"
            }
        };

        // Act
        messageEvent.Media = media;

        // Assert
        Assert.Equal(2, messageEvent.Media.Count);
        Assert.Equal("image/jpeg", messageEvent.Media[0].MimeType);
        Assert.Equal(1024, messageEvent.Media[0].Size);
        Assert.Equal("application/pdf", messageEvent.Media[1].MimeType);
        Assert.Equal("doc.pdf", messageEvent.Media[1].FileName);
    }

    [Fact]
    public void Reactions_ShouldHandleCollection()
    {
        // Arrange
        var messageEvent = new MessageEvent();
        var reactions = new List<MessageReactionInfo>
        {
            new MessageReactionInfo { Key = "👍", Count = 5 },
            new MessageReactionInfo { Key = "❤️", Count = 3 }
        };

        // Act
        messageEvent.Reactions = reactions;

        // Assert
        Assert.Equal(2, messageEvent.Reactions.Count);
        Assert.Equal("👍", messageEvent.Reactions[0].Key);
        Assert.Equal(5, messageEvent.Reactions[0].Count);
        Assert.Equal("❤️", messageEvent.Reactions[1].Key);
        Assert.Equal(3, messageEvent.Reactions[1].Count);
    }

    [Fact]
    public void Raw_ShouldHandleJsonDocument()
    {
        // Arrange
        var messageEvent = new MessageEvent();
        var json = """{"custom":"data","nested":{"value":123}}""";
        var document = JsonDocument.Parse(json);

        // Act
        messageEvent.Raw = document;

        // Assert
        Assert.NotNull(messageEvent.Raw);
        Assert.Equal("data", messageEvent.Raw.RootElement.GetProperty("custom").GetString());
        Assert.Equal(123, messageEvent.Raw.RootElement.GetProperty("nested").GetProperty("value").GetInt32());

        // Cleanup
        document.Dispose();
    }

    [Fact]
    public void MessageEventType_ShouldHaveExpectedValues()
    {
        // Assert enum values exist
        Assert.Equal(0, (int)MessageEventType.Text);
        Assert.Equal(1, (int)MessageEventType.Image);
        Assert.Equal(2, (int)MessageEventType.File);
        Assert.Equal(3, (int)MessageEventType.Poll);
        Assert.Equal(4, (int)MessageEventType.System);
        Assert.Equal(5, (int)MessageEventType.Reaction);
        Assert.Equal(6, (int)MessageEventType.Unknown);
    }

    [Fact]
    public void FragmentType_ShouldHaveExpectedValues()
    {
        // Assert enum values exist
        Assert.Equal(0, (int)FragmentType.Link);
        Assert.Equal(1, (int)FragmentType.Mention);
        Assert.Equal(2, (int)FragmentType.Emoji);
        Assert.Equal(3, (int)FragmentType.Hashtag);
        Assert.Equal(4, (int)FragmentType.Code);
    }
}

public class MessageSenderInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var sender = new MessageSenderInfo();

        // Assert
        Assert.Equal(string.Empty, sender.PlatformUserId);
        Assert.Null(sender.DisplayName);
        Assert.False(sender.IsBot);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var sender = new MessageSenderInfo();

        // Act
        sender.PlatformUserId = "user123";
        sender.DisplayName = "John Doe";
        sender.IsBot = true;

        // Assert
        Assert.Equal("user123", sender.PlatformUserId);
        Assert.Equal("John Doe", sender.DisplayName);
        Assert.True(sender.IsBot);
    }
}

public class MessageFragmentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var fragment = new MessageFragment();

        // Assert
        Assert.Equal(FragmentType.Link, fragment.Type); // Default enum value
        Assert.Equal(string.Empty, fragment.Value);
        Assert.Null(fragment.Url);
        Assert.Equal(0, fragment.StartIndex);
        Assert.Equal(0, fragment.EndIndex);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var fragment = new MessageFragment();

        // Act
        fragment.Type = FragmentType.Mention;
        fragment.Value = "@johndoe";
        fragment.Url = "https://slack.com/user/123";
        fragment.StartIndex = 10;
        fragment.EndIndex = 18;

        // Assert
        Assert.Equal(FragmentType.Mention, fragment.Type);
        Assert.Equal("@johndoe", fragment.Value);
        Assert.Equal("https://slack.com/user/123", fragment.Url);
        Assert.Equal(10, fragment.StartIndex);
        Assert.Equal(18, fragment.EndIndex);
    }

    [Theory]
    [InlineData(FragmentType.Link, "https://example.com", "https://example.com", 0, 19)]
    [InlineData(FragmentType.Mention, "@user", null, 5, 10)]
    [InlineData(FragmentType.Emoji, "😀", null, 15, 17)]
    [InlineData(FragmentType.Hashtag, "#trending", null, 20, 29)]
    [InlineData(FragmentType.Code, "`console.log()`", null, 30, 45)]
    public void Fragment_ShouldHandleVariousTypes(FragmentType type, string value, string? url, int start, int end)
    {
        // Arrange & Act
        var fragment = new MessageFragment
        {
            Type = type,
            Value = value,
            Url = url,
            StartIndex = start,
            EndIndex = end
        };

        // Assert
        Assert.Equal(type, fragment.Type);
        Assert.Equal(value, fragment.Value);
        Assert.Equal(url, fragment.Url);
        Assert.Equal(start, fragment.StartIndex);
        Assert.Equal(end, fragment.EndIndex);
    }
}

public class MessageMediaTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var media = new MessageMedia();

        // Assert
        Assert.Equal(string.Empty, media.Url);
        Assert.Equal(string.Empty, media.MimeType);
        Assert.Null(media.Size);
        Assert.Null(media.FileName);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var media = new MessageMedia();

        // Act
        media.Url = "https://example.com/file.pdf";
        media.MimeType = "application/pdf";
        media.Size = 1048576;
        media.FileName = "document.pdf";

        // Assert
        Assert.Equal("https://example.com/file.pdf", media.Url);
        Assert.Equal("application/pdf", media.MimeType);
        Assert.Equal(1048576, media.Size);
        Assert.Equal("document.pdf", media.FileName);
    }

    [Theory]
    [InlineData("image/jpeg", "photo.jpg", 102400)]
    [InlineData("video/mp4", "video.mp4", 5242880)]
    [InlineData("audio/mpeg", "song.mp3", 3145728)]
    [InlineData("application/zip", "archive.zip", 2097152)]
    public void Media_ShouldHandleVariousTypes(string mimeType, string fileName, long size)
    {
        // Arrange & Act
        var media = new MessageMedia
        {
            Url = $"https://cdn.example.com/{fileName}",
            MimeType = mimeType,
            FileName = fileName,
            Size = size
        };

        // Assert
        Assert.Equal($"https://cdn.example.com/{fileName}", media.Url);
        Assert.Equal(mimeType, media.MimeType);
        Assert.Equal(fileName, media.FileName);
        Assert.Equal(size, media.Size);
    }
}

public class MessageReactionInfoTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var reaction = new MessageReactionInfo();

        // Assert
        Assert.Equal(string.Empty, reaction.Key);
        Assert.Equal(0, reaction.Count);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var reaction = new MessageReactionInfo();

        // Act
        reaction.Key = "🎉";
        reaction.Count = 10;

        // Assert
        Assert.Equal("🎉", reaction.Key);
        Assert.Equal(10, reaction.Count);
    }

    [Theory]
    [InlineData("👍", 5)]
    [InlineData("❤️", 15)]
    [InlineData("😂", 3)]
    [InlineData(":custom_emoji:", 1)]
    public void Reaction_ShouldHandleVariousEmojis(string key, int count)
    {
        // Arrange & Act
        var reaction = new MessageReactionInfo
        {
            Key = key,
            Count = count
        };

        // Assert
        Assert.Equal(key, reaction.Key);
        Assert.Equal(count, reaction.Count);
    }
}