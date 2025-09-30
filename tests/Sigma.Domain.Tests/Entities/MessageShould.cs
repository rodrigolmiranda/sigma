using Sigma.Domain.Entities;
using Sigma.Domain.ValueObjects;
using Xunit;

namespace Sigma.Domain.Tests.Entities;

public class MessageShould
{
    [Fact]
    public void BeCreatedWithValidData()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var platformMessageId = "MSG123";
        var sender = new MessageSender("U123", "John Doe", false);
        var type = MessageType.Text;
        var text = "Hello, world!";
        var timestamp = DateTime.UtcNow;

        // Act
        var message = new Message(channelId, tenantId, platformMessageId, sender, type, text, timestamp);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(channelId, message.ChannelId);
        Assert.Equal(tenantId, message.TenantId);
        Assert.Equal(platformMessageId, message.PlatformMessageId);
        Assert.Equal(sender, message.Sender);
        Assert.Equal(type, message.Type);
        Assert.Equal(text, message.Text);
        Assert.Equal(timestamp, message.TimestampUtc);
        Assert.False(message.IsDeleted);
        Assert.Null(message.EditedAtUtc);
        Assert.Null(message.ReplyToPlatformMessageId);
        Assert.Empty(message.Reactions);
    }

    [Fact]
    public void ThrowWhenCreatedWithNullPlatformMessageId()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Message(Guid.NewGuid(), Guid.NewGuid(), null!, sender, MessageType.Text, "text", DateTime.UtcNow));
    }

    [Fact]
    public void ThrowWhenCreatedWithNullSender()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", null!, MessageType.Text, "text", DateTime.UtcNow));
    }

    [Fact]
    public void AcceptNullText()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);

        // Act
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Image, null, DateTime.UtcNow);

        // Assert
        Assert.Null(message.Text);
    }

    [Fact]
    public void MarkAsEditedSuccessfully()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Text, "Original", DateTime.UtcNow);
        var newText = "Edited text";
        var editedAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        message.MarkAsEdited(newText, editedAt);

        // Assert
        Assert.Equal(newText, message.Text);
        Assert.Equal(editedAt, message.EditedAtUtc);
    }

    [Fact]
    public void MarkAsDeletedSuccessfully()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Text, "Text", DateTime.UtcNow);
        Assert.False(message.IsDeleted);

        // Act
        message.MarkAsDeleted();

        // Assert
        Assert.True(message.IsDeleted);
    }

    [Fact]
    public void AddReactionSuccessfully()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Text, "Text", DateTime.UtcNow);

        // Act
        message.AddReaction("👍", 5);

        // Assert
        Assert.Single(message.Reactions);
        var reaction = message.Reactions.First();
        Assert.Equal("👍", reaction.Key);
        Assert.Equal(5, reaction.Count);
    }

    [Fact]
    public void UpdateExistingReaction()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Text, "Text", DateTime.UtcNow);
        message.AddReaction("👍", 5);

        // Act
        message.AddReaction("👍", 10);

        // Assert
        Assert.Single(message.Reactions);
        var reaction = message.Reactions.First();
        Assert.Equal("👍", reaction.Key);
        Assert.Equal(10, reaction.Count);
    }

    [Fact]
    public void AddMultipleReactions()
    {
        // Arrange
        var sender = new MessageSender("U123", "John", false);
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "MSG123", sender, MessageType.Text, "Text", DateTime.UtcNow);

        // Act
        message.AddReaction("👍", 5);
        message.AddReaction("❤️", 3);
        message.AddReaction("😂", 7);

        // Assert
        Assert.Equal(3, message.Reactions.Count);
        Assert.Contains(message.Reactions, r => r.Key == "👍" && r.Count == 5);
        Assert.Contains(message.Reactions, r => r.Key == "❤️" && r.Count == 3);
        Assert.Contains(message.Reactions, r => r.Key == "😂" && r.Count == 7);
    }
}