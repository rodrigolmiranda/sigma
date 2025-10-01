using System.Reflection;
using FluentAssertions;
using Sigma.Domain.Entities;
using Sigma.Domain.ValueObjects;
using Xunit;

namespace Sigma.Domain.Tests.Entities;

public class EntityPrivateConstructorTests
{
    [Fact]
    public void Channel_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(Channel);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as Channel;

        // Assert
        instance.Should().NotBeNull();
        instance!.Name.Should().Be(string.Empty);
        instance.ExternalId.Should().Be(string.Empty);
        instance.WorkspaceId.Should().Be(Guid.Empty);
        instance.IsActive.Should().BeFalse();
        instance.LastMessageAtUtc.Should().BeNull();
        instance.RetentionOverrideDays.Should().BeNull();
        instance.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Tenant_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(Tenant);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as Tenant;

        // Assert
        instance.Should().NotBeNull();
        instance!.Name.Should().Be(string.Empty);
        instance.Slug.Should().Be(string.Empty);
        instance.PlanType.Should().Be("free");  // Default value set in constructor
        instance.IsActive.Should().BeTrue();
        instance.RetentionDays.Should().Be(30);
        instance.Workspaces.Should().BeEmpty();
    }

    [Fact]
    public void Workspace_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(Workspace);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as Workspace;

        // Assert
        instance.Should().NotBeNull();
        instance!.Name.Should().Be(string.Empty);
        instance.ExternalId.Should().BeNull();  // Not initialized in private constructor
        instance.TenantId.Should().Be(Guid.Empty);
        instance.IsActive.Should().BeTrue();
        instance.Channels.Should().BeEmpty();
    }

    [Fact]
    public void Message_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(Message);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as Message;

        // Assert
        instance.Should().NotBeNull();
        instance!.Text.Should().BeNull(); // Text is nullable and default is null
        instance.PlatformMessageId.Should().Be(string.Empty);
        instance.ChannelId.Should().Be(Guid.Empty);
        instance.TenantId.Should().Be(Guid.Empty);
        instance.Sender.Should().BeNull(); // EF Core initializes this
        instance.Type.Should().Be(default);
        instance.TimestampUtc.Should().Be(default);
        instance.EditedAtUtc.Should().BeNull();
        instance.IsDeleted.Should().BeFalse();
        instance.ReplyToPlatformMessageId.Should().BeNull();
        instance.Reactions.Should().BeEmpty();
    }

    [Fact]
    public void MessageReaction_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(MessageReaction);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as MessageReaction;

        // Assert
        instance.Should().NotBeNull();
        instance!.Key.Should().Be(string.Empty);
        instance.Count.Should().Be(0);
    }

    [Fact]
    public void MessageSender_PrivateConstructor_ShouldInitializeWithDefaults()
    {
        // Arrange
        var type = typeof(MessageSender);
        var constructor = type.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        // Act
        var instance = constructor!.Invoke(null) as MessageSender;

        // Assert
        instance.Should().NotBeNull();
        instance!.PlatformUserId.Should().Be(string.Empty);
        instance.DisplayName.Should().BeNull();
        instance.IsBot.Should().BeFalse();
    }

    [Fact]
    public void AllPrivateConstructors_ShouldBeAccessibleForEFCore()
    {
        // This test verifies that all domain entities have private parameterless constructors
        // which are required for Entity Framework Core to materialize entities from the database

        var entityTypes = new[]
        {
            typeof(Channel),
            typeof(Tenant),
            typeof(Workspace),
            typeof(Message)
        };

        foreach (var entityType in entityTypes)
        {
            var constructor = entityType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            constructor.Should().NotBeNull($"{entityType.Name} should have a private parameterless constructor for EF Core");
            constructor!.IsPrivate.Should().BeTrue($"{entityType.Name} constructor should be private");
        }
    }

    [Fact]
    public void AllValueObjects_ShouldHavePrivateConstructors()
    {
        // This test verifies that all value objects have private parameterless constructors
        // which are required for Entity Framework Core to materialize value objects

        var valueObjectTypes = new[]
        {
            typeof(MessageReaction),
            typeof(MessageSender)
        };

        foreach (var voType in valueObjectTypes)
        {
            var constructor = voType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            constructor.Should().NotBeNull($"{voType.Name} should have a private parameterless constructor for EF Core");
            constructor!.IsPrivate.Should().BeTrue($"{voType.Name} constructor should be private");
        }
    }
}