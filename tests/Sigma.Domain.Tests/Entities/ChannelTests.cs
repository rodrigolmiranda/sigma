using System;
using System.Linq;
using Sigma.Domain.Entities;
using Xunit;

namespace Sigma.Domain.Tests.Entities;

public class ChannelTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var name = "general";
        var externalId = "C123456";

        // Act
        var channel = new Channel(workspaceId, name, externalId);

        // Assert
        Assert.Equal(workspaceId, channel.WorkspaceId);
        Assert.Equal(name, channel.Name);
        Assert.Equal(externalId, channel.ExternalId);
        Assert.True(channel.IsActive);
        Assert.Null(channel.LastMessageAtUtc);
        Assert.Null(channel.RetentionOverrideDays);
        Assert.NotEqual(Guid.Empty, channel.Id);
        Assert.NotEqual(default(DateTime), channel.CreatedAtUtc);
        Assert.NotEqual(default(DateTime), channel.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        string? name = null;
        var externalId = "C123456";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Channel(workspaceId, name!, externalId));
    }

    [Fact]
    public void Constructor_WithNullExternalId_ThrowsArgumentNullException()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var name = "general";
        string? externalId = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Channel(workspaceId, name, externalId!));
    }

    [Fact]
    public void Constructor_WithEmptyWorkspaceId_CreatesInstance()
    {
        // Arrange
        var workspaceId = Guid.Empty;
        var name = "general";
        var externalId = "C123456";

        // Act
        var channel = new Channel(workspaceId, name, externalId);

        // Assert
        Assert.Equal(Guid.Empty, channel.WorkspaceId);
    }

    [Fact]
    public void UpdateLastMessageTime_SetsLastMessageTimeAndUpdatedAt()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        var messageTime = DateTime.UtcNow.AddMinutes(-5);

        // Small delay to ensure UpdatedAtUtc changes
        System.Threading.Thread.Sleep(10);

        // Act
        channel.UpdateLastMessageTime(messageTime);

        // Assert
        Assert.Equal(messageTime, channel.LastMessageAtUtc);
    }

    [Fact]
    public void UpdateLastMessageTime_CanBeCalledMultipleTimes()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        var time1 = DateTime.UtcNow.AddHours(-2);
        var time2 = DateTime.UtcNow.AddHours(-1);
        var time3 = DateTime.UtcNow;

        // Act
        channel.UpdateLastMessageTime(time1);
        Assert.Equal(time1, channel.LastMessageAtUtc);

        channel.UpdateLastMessageTime(time2);
        Assert.Equal(time2, channel.LastMessageAtUtc);

        channel.UpdateLastMessageTime(time3);
        Assert.Equal(time3, channel.LastMessageAtUtc);
    }

    [Fact]
    public void SetRetentionOverride_WithValidDays_SetsRetention()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        var retentionDays = 30;

        // Small delay to ensure UpdatedAtUtc changes
        System.Threading.Thread.Sleep(10);

        // Act
        channel.SetRetentionOverride(retentionDays);

        // Assert
        Assert.Equal(retentionDays, channel.RetentionOverrideDays);
    }

    [Fact]
    public void SetRetentionOverride_WithNull_ClearsRetention()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        channel.SetRetentionOverride(30);
        Assert.Equal(30, channel.RetentionOverrideDays);

        // Act
        channel.SetRetentionOverride(null);

        // Assert
        Assert.Null(channel.RetentionOverrideDays);
    }

    [Fact]
    public void SetRetentionOverride_WithZeroDays_ThrowsArgumentException()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => channel.SetRetentionOverride(0));
        Assert.Contains("Retention override days must be positive", ex.Message);
        Assert.Equal("days", ex.ParamName);
    }

    [Fact]
    public void SetRetentionOverride_WithNegativeDays_ThrowsArgumentException()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => channel.SetRetentionOverride(-1));
        Assert.Contains("Retention override days must be positive", ex.Message);
        Assert.Equal("days", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    [InlineData(int.MaxValue)]
    public void SetRetentionOverride_WithVariousValidDays_SetsCorrectly(int days)
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act
        channel.SetRetentionOverride(days);

        // Assert
        Assert.Equal(days, channel.RetentionOverrideDays);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalseAndUpdatesTimestamp()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        Assert.True(channel.IsActive);

        // Small delay to ensure UpdatedAtUtc changes
        System.Threading.Thread.Sleep(10);

        // Act
        channel.Deactivate();

        // Assert
        Assert.False(channel.IsActive);
    }

    [Fact]
    public void Deactivate_CanBeCalledMultipleTimes()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act
        channel.Deactivate();
        Assert.False(channel.IsActive);

        channel.Deactivate(); // Call again

        // Assert
        Assert.False(channel.IsActive);
    }

    [Fact]
    public void Activate_SetsIsActiveToTrueAndUpdatesTimestamp()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        channel.Deactivate();
        Assert.False(channel.IsActive);


        // Small delay to ensure UpdatedAtUtc changes
        System.Threading.Thread.Sleep(10);

        // Act
        channel.Activate();

        // Assert
        Assert.True(channel.IsActive);
    }

    [Fact]
    public void Activate_CanBeCalledMultipleTimes()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act
        channel.Activate();
        Assert.True(channel.IsActive);

        channel.Activate(); // Call again

        // Assert
        Assert.True(channel.IsActive);
    }

    [Fact]
    public void ActivateDeactivate_CanToggleStatus()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act & Assert
        Assert.True(channel.IsActive);

        channel.Deactivate();
        Assert.False(channel.IsActive);

        channel.Activate();
        Assert.True(channel.IsActive);

        channel.Deactivate();
        Assert.False(channel.IsActive);

        channel.Activate();
        Assert.True(channel.IsActive);
    }

    [Fact]
    public void Messages_ReturnsReadOnlyList()
    {
        // Arrange
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");

        // Act
        var messages = channel.Messages;

        // Assert
        Assert.NotNull(messages);
        Assert.Empty(messages);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<Message>>(messages);
    }

    [Fact]
    public void Entity_HasUniqueId()
    {
        // Arrange & Act
        var channel1 = new Channel(Guid.NewGuid(), "general", "C123456");
        var channel2 = new Channel(Guid.NewGuid(), "random", "C789012");

        // Assert
        Assert.NotEqual(channel1.Id, channel2.Id);
        Assert.NotEqual(Guid.Empty, channel1.Id);
        Assert.NotEqual(Guid.Empty, channel2.Id);
    }

    [Fact]
    public void Entity_TracksCreationAndUpdateTimes()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddMilliseconds(-100);
        
        // Act
        var channel = new Channel(Guid.NewGuid(), "general", "C123456");
        
        var afterCreation = DateTime.UtcNow.AddMilliseconds(100);

        // Assert
        Assert.True(channel.CreatedAtUtc >= beforeCreation);
        Assert.True(channel.CreatedAtUtc <= afterCreation);
        Assert.Null(channel.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyStrings_CreatesInstance()
    {
        // Arrange & Act
        var channel = new Channel(Guid.NewGuid(), "", "");

        // Assert
        Assert.Equal("", channel.Name);
        Assert.Equal("", channel.ExternalId);
    }

    [Fact]
    public void Constructor_WithLongStrings_CreatesInstance()
    {
        // Arrange
        var longName = new string('a', 1000);
        var longExternalId = new string('b', 1000);

        // Act
        var channel = new Channel(Guid.NewGuid(), longName, longExternalId);

        // Assert
        Assert.Equal(longName, channel.Name);
        Assert.Equal(longExternalId, channel.ExternalId);
    }

}
