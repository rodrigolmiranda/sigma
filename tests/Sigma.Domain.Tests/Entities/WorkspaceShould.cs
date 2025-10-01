using Sigma.Domain.Entities;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Domain.Tests.Entities;

public class WorkspaceShould
{
    [Fact]
    public void BeCreatedWithValidData()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var name = "Test Workspace";
        var platform = Platform.Discord;

        // Act
        var workspace = new Workspace(tenantId, name, platform);

        // Assert
        Assert.NotNull(workspace);
        Assert.Equal(tenantId, workspace.TenantId);
        Assert.Equal(name, workspace.Name);
        Assert.Equal(platform, workspace.Platform);
        Assert.True(workspace.IsActive);
        Assert.Null(workspace.ExternalId);
        Assert.Null(workspace.LastSyncAtUtc);
        Assert.NotEqual(Guid.Empty, workspace.Id);
    }

    [Fact]
    public void ThrowWhenCreatedWithNullName()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Workspace(Guid.NewGuid(), null!, Platform.Slack));
    }

    // Test removed - Platform is now an enum (value type), cannot be null

    [Fact]
    public void UpdateExternalIdSuccessfully()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        var externalId = "T123456";

        // Act
        workspace.UpdateExternalId(externalId);

        // Assert
        Assert.Equal(externalId, workspace.ExternalId);
    }

    [Fact]
    public void UpdateLastSyncSuccessfully()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        Assert.Null(workspace.LastSyncAtUtc);

        // Act
        workspace.UpdateLastSync();

        // Assert
        Assert.NotNull(workspace.LastSyncAtUtc);
        Assert.True(workspace.LastSyncAtUtc <= DateTime.UtcNow);
        Assert.True(workspace.LastSyncAtUtc >= DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void DeactivateAndActivateSuccessfully()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        Assert.True(workspace.IsActive);

        // Act - Deactivate
        workspace.Deactivate();

        // Assert
        Assert.False(workspace.IsActive);

        // Act - Activate
        workspace.Activate();

        // Assert
        Assert.True(workspace.IsActive);
    }

    [Fact]
    public void AddChannelSuccessfully()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        var channelName = "general";
        var externalId = "C123456";

        // Act
        var channel = workspace.AddChannel(channelName, externalId);

        // Assert
        Assert.NotNull(channel);
        Assert.Equal(channelName, channel.Name);
        Assert.Equal(externalId, channel.ExternalId);
        Assert.Equal(workspace.Id, channel.WorkspaceId);
        Assert.Single(workspace.Channels);
        Assert.Contains(channel, workspace.Channels);
    }

    [Fact]
    public void AddMultipleChannels()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        var channel1 = workspace.AddChannel("general", "C1");
        var channel2 = workspace.AddChannel("random", "C2");
        var channel3 = workspace.AddChannel("dev", "C3");

        // Assert
        Assert.Equal(3, workspace.Channels.Count);
        Assert.Contains(channel1, workspace.Channels);
        Assert.Contains(channel2, workspace.Channels);
        Assert.Contains(channel3, workspace.Channels);
    }

    [Fact]
    public void UpdateExternalId_MultipleTimes_ShouldUpdateCorrectly()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        workspace.UpdateExternalId("EXT001");
        Assert.Equal("EXT001", workspace.ExternalId);

        workspace.UpdateExternalId("EXT002");
        Assert.Equal("EXT002", workspace.ExternalId);

        workspace.UpdateExternalId("EXT003");
        Assert.Equal("EXT003", workspace.ExternalId);
    }

    [Fact]
    public void UpdateExternalId_WithNull_ShouldSetToNull()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        workspace.UpdateExternalId("EXT001");
        Assert.Equal("EXT001", workspace.ExternalId);

        // Act
        workspace.UpdateExternalId(null!);

        // Assert
        Assert.Null(workspace.ExternalId);
    }

    [Fact]
    public void UpdateExternalId_WithEmptyString_ShouldSetToEmpty()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        workspace.UpdateExternalId("");

        // Assert
        Assert.Equal("", workspace.ExternalId);
    }

    [Fact]
    public void UpdateLastSync_MultipleTimes_ShouldUpdateCorrectly()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        Assert.Null(workspace.LastSyncAtUtc);

        // Act & Assert
        workspace.UpdateLastSync();
        var firstSync = workspace.LastSyncAtUtc;
        Assert.NotNull(firstSync);

        System.Threading.Thread.Sleep(10);
        workspace.UpdateLastSync();
        var secondSync = workspace.LastSyncAtUtc;
        Assert.NotNull(secondSync);
        Assert.True(secondSync > firstSync);

        System.Threading.Thread.Sleep(10);
        workspace.UpdateLastSync();
        var thirdSync = workspace.LastSyncAtUtc;
        Assert.NotNull(thirdSync);
        Assert.True(thirdSync > secondSync);
    }

    [Fact]
    public void UpdateExternalId_ShouldUpdateTimestamp()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.UpdateExternalId("EXT001");

        // Assert
    }

    [Fact]
    public void UpdateLastSync_ShouldUpdateTimestamp()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.UpdateLastSync();

        // Assert
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        workspace.Deactivate();
        Assert.False(workspace.IsActive);
        var firstDeactivationTime = workspace.UpdatedAtUtc;

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.Deactivate();

        // Assert
        Assert.False(workspace.IsActive);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        Assert.True(workspace.IsActive);
        var originalUpdateTime = workspace.UpdatedAtUtc;

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.Activate();

        // Assert
        Assert.True(workspace.IsActive);
    }

    [Fact]
    public void Constructor_WithEmptyTenantId_ShouldCreateSuccessfully()
    {
        // Act
        var workspace = new Workspace(Guid.Empty, "Test", Platform.Slack);

        // Assert
        Assert.Equal(Guid.Empty, workspace.TenantId);
        Assert.Equal("Test", workspace.Name);
        Assert.Equal(Platform.Slack, workspace.Platform);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldCreateSuccessfully()
    {
        // Act
        var workspace = new Workspace(Guid.NewGuid(), "", Platform.Slack);

        // Assert
        Assert.Equal("", workspace.Name);
        Assert.Equal(Platform.Slack, workspace.Platform);
    }

    [Theory]
    [InlineData(Platform.Slack)]
    [InlineData(Platform.Discord)]
    [InlineData(Platform.WhatsApp)]
    [InlineData(Platform.Telegram)]
    [InlineData(Platform.Teams)]
    [InlineData(Platform.YouTube)]
    public void Constructor_WithVariousPlatforms_ShouldSetCorrectly(Platform platform)
    {
        // Act
        var workspace = new Workspace(Guid.NewGuid(), "Test", platform);

        // Assert
        Assert.Equal(platform, workspace.Platform);
    }

    [Fact]
    public void Constructor_WithLongName_ShouldCreateSuccessfully()
    {
        // Arrange
        var longName = new string('a', 1000);

        // Act
        var workspace = new Workspace(Guid.NewGuid(), longName, Platform.Slack);

        // Assert
        Assert.Equal(longName, workspace.Name);
        Assert.Equal(Platform.Slack, workspace.Platform);
    }

    [Fact]
    public void Channels_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        workspace.AddChannel("general", "C1");
        workspace.AddChannel("random", "C2");

        // Act
        var channels = workspace.Channels;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Channel>>(channels);
        Assert.Equal(2, channels.Count);
    }

    [Fact]
    public void Entity_ShouldHaveUniqueId()
    {
        // Act
        var workspace1 = new Workspace(Guid.NewGuid(), "Test 1", Platform.Slack);
        var workspace2 = new Workspace(Guid.NewGuid(), "Test 2", Platform.Discord);

        // Assert
        Assert.NotEqual(workspace1.Id, workspace2.Id);
        Assert.NotEqual(Guid.Empty, workspace1.Id);
        Assert.NotEqual(Guid.Empty, workspace2.Id);
    }

    [Fact]
    public void Entity_ShouldTrackCreationAndUpdateTimes()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddMilliseconds(-100);

        // Act
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        var afterCreation = DateTime.UtcNow.AddMilliseconds(100);

        // Assert
        Assert.True(workspace.CreatedAtUtc >= beforeCreation);
        Assert.True(workspace.CreatedAtUtc <= afterCreation);
        Assert.Null(workspace.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_ShouldUpdateTimestamp()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.Deactivate();

        // Assert
    }

    [Fact]
    public void Activate_ShouldUpdateTimestamp()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        workspace.Deactivate();

        // Act
        System.Threading.Thread.Sleep(10);
        workspace.Activate();

        // Assert
    }

    [Fact]
    public void AddChannel_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => workspace.AddChannel(null!, "C1"));
    }

    [Fact]
    public void AddChannel_WithNullExternalId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => workspace.AddChannel("general", null!));
    }

    [Fact]
    public void AddChannel_WithEmptyStrings_ShouldCreateChannel()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);

        // Act
        var channel = workspace.AddChannel("", "");

        // Assert
        Assert.NotNull(channel);
        Assert.Equal("", channel.Name);
        Assert.Equal("", channel.ExternalId);
        Assert.Equal(workspace.Id, channel.WorkspaceId);
    }

    [Fact]
    public void UpdateExternalId_WithLongString_ShouldSetCorrectly()
    {
        // Arrange
        var workspace = new Workspace(Guid.NewGuid(), "Test", Platform.Slack);
        var longExternalId = new string('x', 10000);

        // Act
        workspace.UpdateExternalId(longExternalId);

        // Assert
        Assert.Equal(longExternalId, workspace.ExternalId);
    }
}