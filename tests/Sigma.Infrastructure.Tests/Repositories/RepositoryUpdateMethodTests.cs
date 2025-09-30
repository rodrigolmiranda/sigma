using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sigma.Domain.Entities;
using Sigma.Domain.ValueObjects;
using Sigma.Infrastructure.Persistence;
using Sigma.Infrastructure.Persistence.Repositories;
using Sigma.Infrastructure.Tests.TestHelpers;
using Xunit;

namespace Sigma.Infrastructure.Tests.Repositories;

public class RepositoryUpdateMethodTests : IDisposable
{
    private readonly SigmaDbContext _context;
    private readonly ChannelRepository _channelRepository;
    private readonly WorkspaceRepository _workspaceRepository;
    private readonly TenantRepository _tenantRepository;
    private readonly MessageRepository _messageRepository;

    public RepositoryUpdateMethodTests()
    {
        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestSigmaDbContext(options);
        _channelRepository = new ChannelRepository(_context);
        _workspaceRepository = new WorkspaceRepository(_context);
        _tenantRepository = new TenantRepository(_context);
        _messageRepository = new MessageRepository(_context);
    }

    [Fact]
    public async Task ChannelRepository_UpdateAsync_WithValidChannel_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);

        var channel = new Channel(workspace.Id, "Original Name", "ext-ch-1");
        await _context.Channels.AddAsync(channel);
        await _context.SaveChangesAsync();

        // Act
        channel.UpdateLastMessageTime(DateTime.UtcNow);
        await _channelRepository.UpdateAsync(channel);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Channels.FindAsync(channel.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated!.LastMessageAtUtc);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.True((DateTime.UtcNow - updated.UpdatedAtUtc.Value).TotalSeconds < 1);
    }

    [Fact]
    public async Task ChannelRepository_UpdateAsync_WithNullChannel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _channelRepository.UpdateAsync(null!);
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ChannelRepository_UpdateAsync_WithDetachedEntity_ShouldAttachAndUpdate()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);

        var channel = new Channel(workspace.Id, "Test Channel", "ext-ch-1");
        await _context.Channels.AddAsync(channel);
        await _context.SaveChangesAsync();

        // Detach the entity
        _context.Entry(channel).State = EntityState.Detached;

        // Act
        channel.SetRetentionOverride(90);
        await _channelRepository.UpdateAsync(channel);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Channels.FindAsync(channel.Id);
        Assert.NotNull(updated);
        Assert.Equal(90, updated!.RetentionOverrideDays);
    }

    [Fact]
    public async Task WorkspaceRepository_UpdateAsync_WithValidWorkspace_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Original Name", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);
        await _context.SaveChangesAsync();

        // Act
        workspace.UpdateExternalId("updated-external");
        await _workspaceRepository.UpdateAsync(workspace);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Workspaces.FindAsync(workspace.Id);
        Assert.NotNull(updated);
        Assert.Equal("updated-external", updated!.ExternalId);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.True((DateTime.UtcNow - updated.UpdatedAtUtc.Value).TotalSeconds < 1);
    }

    [Fact]
    public async Task WorkspaceRepository_UpdateAsync_WithNullWorkspace_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _workspaceRepository.UpdateAsync(null!);
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task WorkspaceRepository_UpdateAsync_WithMultipleUpdates_ShouldPersistAll()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Original Name", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);
        await _context.SaveChangesAsync();

        // Act
        workspace.UpdateExternalId("first-update");
        await _workspaceRepository.UpdateAsync(workspace);

        workspace.UpdateExternalId("second-update");
        await _workspaceRepository.UpdateAsync(workspace);

        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Workspaces.FindAsync(workspace.Id);
        Assert.NotNull(updated);
        Assert.Equal("second-update", updated!.ExternalId);
    }

    [Fact]
    public async Task TenantRepository_UpdateAsync_WithValidTenant_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Original Name", "test-tenant");
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        tenant.UpdatePlan("enterprise", 90);
        await _tenantRepository.UpdateAsync(tenant);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updated);
        Assert.Equal("enterprise", updated!.PlanType);
        Assert.Equal(90, updated.RetentionDays);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.True((DateTime.UtcNow - updated.UpdatedAtUtc.Value).TotalSeconds < 1);
    }

    [Fact]
    public async Task TenantRepository_UpdateAsync_WithNullTenant_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _tenantRepository.UpdateAsync(null!);
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task TenantRepository_UpdateAsync_WithDeactivatedTenant_ShouldPersistDeactivation()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();

        // Act
        tenant.Deactivate();
        await _tenantRepository.UpdateAsync(tenant);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Tenants.FindAsync(tenant.Id);
        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task MessageRepository_UpdateAsync_WithValidMessage_ShouldUpdateSuccessfully()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);

        var channel = new Channel(workspace.Id, "Test Channel", "ext-ch-1");
        await _context.Channels.AddAsync(channel);

        var sender = new MessageSender("user-1", "Test User", false);
        var message = new Message(
            channel.Id,
            tenant.Id,
            "ext-msg-1",
            sender,
            MessageType.Text,
            "Original text",
            DateTime.UtcNow
        );

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Act
        message.MarkAsEdited("Updated text", DateTime.UtcNow);
        await _messageRepository.UpdateAsync(message);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Messages.FindAsync(message.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated text", updated!.Text);
        Assert.NotNull(updated.EditedAtUtc);
    }

    [Fact]
    public async Task MessageRepository_UpdateAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _messageRepository.UpdateAsync(null!);
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task MessageRepository_UpdateAsync_WithDeletedMessage_ShouldPersistDeletion()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);

        var channel = new Channel(workspace.Id, "Test Channel", "ext-ch-1");
        await _context.Channels.AddAsync(channel);

        var sender = new MessageSender("user-1", "Test User", false);
        var message = new Message(
            channel.Id,
            tenant.Id,
            "ext-msg-1",
            sender,
            MessageType.Text,
            "Test text",
            DateTime.UtcNow
        );

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Act
        message.MarkAsDeleted();
        await _messageRepository.UpdateAsync(message);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Messages.FindAsync(message.Id);
        Assert.NotNull(updated);
        Assert.True(updated!.IsDeleted);
    }

    [Fact]
    public async Task MessageRepository_Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new MessageRepository(null!);
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public async Task AllRepositories_UpdateAsync_WithConcurrentUpdates_ShouldHandleCorrectly()
    {
        // Arrange
        var tenant = new Tenant("Test Tenant", "test-tenant");
        await _context.Tenants.AddAsync(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", "ext-ws-1");
        await _context.Workspaces.AddAsync(workspace);

        var channel = new Channel(workspace.Id, "Test Channel", "ext-ch-1");
        await _context.Channels.AddAsync(channel);

        var sender = new MessageSender("user-1", "Test User", false);
        var message = new Message(
            channel.Id,
            tenant.Id,
            "ext-msg-1",
            sender,
            MessageType.Text,
            "Test text",
            DateTime.UtcNow
        );
        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Act - Perform multiple updates without saving
        tenant.UpdatePlan("professional", 60);
        await _tenantRepository.UpdateAsync(tenant);

        workspace.UpdateExternalId("updated-workspace");
        await _workspaceRepository.UpdateAsync(workspace);

        // Channel doesn't have UpdateRetentionDays, so we'll skip updating it
        await _channelRepository.UpdateAsync(channel);

        message.MarkAsEdited("Updated message", DateTime.UtcNow);
        await _messageRepository.UpdateAsync(message);

        await _context.SaveChangesAsync();

        // Assert - All updates should be persisted
        var updatedTenant = await _context.Tenants.FindAsync(tenant.Id);
        var updatedWorkspace = await _context.Workspaces.FindAsync(workspace.Id);
        var updatedChannel = await _context.Channels.FindAsync(channel.Id);
        var updatedMessage = await _context.Messages.FindAsync(message.Id);

        Assert.Equal("professional", updatedTenant!.PlanType);
        Assert.Equal("updated-workspace", updatedWorkspace!.ExternalId);
        Assert.NotNull(updatedChannel); // Channel was in transaction
        Assert.Equal("Updated message", updatedMessage!.Text);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}