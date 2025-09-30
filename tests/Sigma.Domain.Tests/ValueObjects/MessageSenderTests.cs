using System;
using System.Linq;
using Sigma.Domain.ValueObjects;
using Xunit;

namespace Sigma.Domain.Tests.ValueObjects;

public class MessageSenderTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var platformUserId = "user123";
        var displayName = "John Doe";
        var isBot = false;

        // Act
        var sender = new MessageSender(platformUserId, displayName, isBot);

        // Assert
        Assert.Equal(platformUserId, sender.PlatformUserId);
        Assert.Equal(displayName, sender.DisplayName);
        Assert.Equal(isBot, sender.IsBot);
    }

    [Fact]
    public void Constructor_WithNullPlatformUserId_ThrowsArgumentNullException()
    {
        // Arrange
        string? platformUserId = null;
        var displayName = "John Doe";
        var isBot = false;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageSender(platformUserId!, displayName, isBot));
    }

    [Fact]
    public void Constructor_WithNullDisplayName_AllowsNullDisplayName()
    {
        // Arrange
        var platformUserId = "user123";
        string? displayName = null;
        var isBot = false;

        // Act
        var sender = new MessageSender(platformUserId, displayName, isBot);

        // Assert
        Assert.Equal(platformUserId, sender.PlatformUserId);
        Assert.Null(sender.DisplayName);
        Assert.Equal(isBot, sender.IsBot);
    }

    [Fact]
    public void Constructor_WithEmptyPlatformUserId_CreatesInstance()
    {
        // Arrange
        var platformUserId = "";
        var displayName = "John Doe";
        var isBot = false;

        // Act
        var sender = new MessageSender(platformUserId, displayName, isBot);

        // Assert
        Assert.Equal(platformUserId, sender.PlatformUserId);
    }

    [Fact]
    public void Unknown_CreatesUnknownUser()
    {
        // Act
        var unknown = MessageSender.Unknown();

        // Assert
        Assert.Equal("unknown", unknown.PlatformUserId);
        Assert.Equal("Unknown User", unknown.DisplayName);
        Assert.False(unknown.IsBot);
    }

    [Fact]
    public void Unknown_CreatesNewInstanceEachTime()
    {
        // Act
        var unknown1 = MessageSender.Unknown();
        var unknown2 = MessageSender.Unknown();

        // Assert
        Assert.NotSame(unknown1, unknown2);
        Assert.Equal(unknown1, unknown2); // Value equality
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user123", "John Doe", false);

        // Act & Assert
        Assert.Equal(sender1, sender2);
        Assert.True(sender1.Equals(sender2));
        Assert.True(sender1 == sender2);
    }

    [Fact]
    public void Equals_WithDifferentPlatformUserId_ReturnsFalse()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user456", "John Doe", false);

        // Act & Assert
        Assert.NotEqual(sender1, sender2);
        Assert.False(sender1.Equals(sender2));
        Assert.True(sender1 != sender2);
    }

    [Fact]
    public void Equals_WithDifferentDisplayName_ReturnsFalse()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user123", "Jane Doe", false);

        // Act & Assert
        Assert.NotEqual(sender1, sender2);
        Assert.False(sender1.Equals(sender2));
    }

    [Fact]
    public void Equals_WithDifferentIsBot_ReturnsFalse()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user123", "John Doe", true);

        // Act & Assert
        Assert.NotEqual(sender1, sender2);
        Assert.False(sender1.Equals(sender2));
    }

    [Fact]
    public void Equals_WithNullDisplayNames_HandlesProperly()
    {
        // Arrange
        var sender1 = new MessageSender("user123", null, false);
        var sender2 = new MessageSender("user123", null, false);
        var sender3 = new MessageSender("user123", "Name", false);

        // Act & Assert
        Assert.Equal(sender1, sender2);
        Assert.NotEqual(sender1, sender3);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user123", "John Doe", false);

        // Act & Assert
        Assert.Equal(sender1.GetHashCode(), sender2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_UsuallyReturnsDifferentHashCode()
    {
        // Arrange
        var sender1 = new MessageSender("user123", "John Doe", false);
        var sender2 = new MessageSender("user456", "Jane Doe", true);

        // Act & Assert
        // Note: Different values don't guarantee different hash codes, but it's very likely
        Assert.NotEqual(sender1.GetHashCode(), sender2.GetHashCode());
    }

    [Theory]
    [InlineData("user123", "John Doe", true)]
    [InlineData("bot456", "Bot Name", true)]
    [InlineData("user789", "Regular User", false)]
    public void IsBot_Property_ReflectsConstructorParameter(string platformUserId, string displayName, bool expectedIsBot)
    {
        // Act
        var sender = new MessageSender(platformUserId, displayName, expectedIsBot);

        // Assert
        Assert.Equal(expectedIsBot, sender.IsBot);
    }

    [Fact]
    public void GetEqualityComponents_ContainsAllProperties()
    {
        // Arrange
        var sender = new MessageSender("user123", "John Doe", true);

        // Act
        // We can't directly test GetEqualityComponents as it's protected,
        // but we can verify it works through equality tests
        var sender2 = new MessageSender("user123", "John Doe", true);
        var sender3 = new MessageSender("user123", "John Doe", false);

        // Assert
        Assert.Equal(sender, sender2);
        Assert.NotEqual(sender, sender3);
    }

    [Fact]
    public void ValueObject_Immutability_PropertiesCannotBeChanged()
    {
        // Arrange
        var sender = new MessageSender("user123", "John Doe", false);

        // Act & Assert
        // Properties should have private setters
        Assert.Equal("user123", sender.PlatformUserId);
        Assert.Equal("John Doe", sender.DisplayName);
        Assert.False(sender.IsBot);
        
        // Verify that a new instance is needed to "change" values
        var newSender = new MessageSender("user456", "Jane Doe", true);
        Assert.NotEqual(sender, newSender);
    }

    [Fact]
    public void Constructor_WithVariousDisplayNames_HandlesCorrectly()
    {
        // Arrange & Act
        var senderWithName = new MessageSender("user1", "Name", false);
        var senderWithEmpty = new MessageSender("user2", "", false);
        var senderWithWhitespace = new MessageSender("user3", "   ", false);
        var senderWithNull = new MessageSender("user4", null, false);

        // Assert
        Assert.Equal("Name", senderWithName.DisplayName);
        Assert.Equal("", senderWithEmpty.DisplayName);
        Assert.Equal("   ", senderWithWhitespace.DisplayName);
        Assert.Null(senderWithNull.DisplayName);
    }

    [Fact]
    public void Constructor_WithLongStrings_HandlesCorrectly()
    {
        // Arrange
        var longUserId = new string('x', 1000);
        var longDisplayName = new string('y', 1000);

        // Act
        var sender = new MessageSender(longUserId, longDisplayName, false);

        // Assert
        Assert.Equal(longUserId, sender.PlatformUserId);
        Assert.Equal(longDisplayName, sender.DisplayName);
    }
}
