using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sigma.Domain.Common;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Tests.TestHelpers;
using Sigma.Domain.ValueObjects;
using Sigma.Infrastructure.Persistence;
using Xunit;

namespace Sigma.Infrastructure.Tests.Persistence;

public class SigmaDbContextTests : IDisposable
{
    private readonly DbContextOptions<SigmaDbContext> _options;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Guid _tenantId;
    private SigmaDbContext? _context;

    public SigmaDbContextTests()
    {
        _tenantId = Guid.NewGuid();
        var databaseName = $"TestDb_{Guid.NewGuid():N}";

        // Create a new service provider for each test to avoid shared caches
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        _options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .UseInternalServiceProvider(serviceProvider)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _tenantContextMock.Setup(x => x.TenantSlug).Returns($"test-tenant-{_tenantId:N}");
    }

    [Fact]
    public void Constructor_WithOptionsOnly_CreatesContext()
    {
        // Act
        using var context = new SigmaDbContext(_options);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Tenants);
        Assert.NotNull(context.Workspaces);
        Assert.NotNull(context.Channels);
        Assert.NotNull(context.Messages);
    }

    [Fact]
    public void Constructor_WithOptionsAndTenantContext_CreatesContext()
    {
        // Act
        using var context = new SigmaDbContext(_options);
        context.TenantContext = _tenantContextMock.Object;

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Tenants);
        Assert.NotNull(context.Workspaces);
        Assert.NotNull(context.Channels);
        Assert.NotNull(context.Messages);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_SetsCreatedAtUtc()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);

        // Act
        var beforeSave = DateTime.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var afterSave = DateTime.UtcNow;

        // Assert
        var savedTenant = await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTenant);
        Assert.InRange(savedTenant.CreatedAtUtc, beforeSave.AddSeconds(-1), afterSave.AddSeconds(1));
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_SetsUpdatedAtUtc()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Modify the tenant
        tenant.UpdatePlan("professional", 90);
        _context.Tenants.Update(tenant);

        // Act
        var beforeUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        var updatedTenant = await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTenant);
        Assert.NotNull(updatedTenant.UpdatedAtUtc);
        Assert.InRange(updatedTenant.UpdatedAtUtc.Value, beforeUpdate.AddSeconds(-1), afterUpdate.AddSeconds(1));
    }

    [Fact]
    public async Task SaveChangesAsync_ClearsDomainEventsAfterSaving()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var workspace = tenant.AddWorkspace("Test Workspace", "Slack");
        _context.Tenants.Add(tenant);

        // Act
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Domain events should be cleared after saving
        var savedTenant = await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedTenant);
        Assert.Empty(savedTenant.DomainEvents);
    }

    [Fact]
    public async Task TenantFilter_AppliedWhenTenantContextProvided()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant1 = new Tenant("Tenant 1", "tenant-1", "free", 30);
        var tenant2 = new Tenant("Tenant 2", "tenant-2", "free", 30);

        // Use reflection to set the tenant IDs to control the test
        typeof(Entity).GetProperty("Id")!.SetValue(tenant1, _tenantId);
        typeof(Entity).GetProperty("Id")!.SetValue(tenant2, Guid.NewGuid());

        _context.Tenants.Add(tenant1);
        _context.Tenants.Add(tenant2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var workspace1 = tenant1.AddWorkspace("Workspace 1", "Slack");
        var workspace2 = tenant2.AddWorkspace("Workspace 2", "Discord");

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Query should be filtered by tenant
        var workspaces = await _context.Workspaces.ToListAsync(TestContext.Current.CancellationToken);

        // Assert - Should only return workspaces for the current tenant
        Assert.Single(workspaces);
        Assert.Equal("Workspace 1", workspaces[0].Name);
        Assert.Equal(_tenantId, workspaces[0].TenantId);
    }

    [Fact]
    public async Task NoTenantFilter_WhenTenantContextIsEmpty()
    {
        // Arrange - Create context with empty tenant ID
        var emptyTenantContextMock = new Mock<ITenantContext>();
        emptyTenantContextMock.Setup(x => x.TenantId).Returns(Guid.Empty);
        emptyTenantContextMock.Setup(x => x.TenantSlug).Returns((string?)null);

        _context = new SigmaDbContext(_options);
        _context.TenantContext = emptyTenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant1 = new Tenant("Tenant 1", "tenant-1", "free", 30);
        var tenant2 = new Tenant("Tenant 2", "tenant-2", "free", 30);
        _context.Tenants.Add(tenant1);
        _context.Tenants.Add(tenant2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var workspace1 = tenant1.AddWorkspace("Workspace 1", "Slack");
        var workspace2 = tenant2.AddWorkspace("Workspace 2", "Discord");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Query should not be filtered when tenant ID is empty
        var workspaces = await _context.Workspaces.ToListAsync(TestContext.Current.CancellationToken);

        // Assert - Should return all workspaces
        Assert.Equal(2, workspaces.Count);
        Assert.Contains(workspaces, w => w.Name == "Workspace 1");
        Assert.Contains(workspaces, w => w.Name == "Workspace 2");
    }

    [Fact]
    public async Task MessageFilter_AppliedWithTenantContext()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        typeof(Entity).GetProperty("Id")!.SetValue(tenant, _tenantId);
        _context.Tenants.Add(tenant);

        var workspace = tenant.AddWorkspace("Test Workspace", "Slack");
        var channel = workspace.AddChannel("general", "C123456");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sender1 = new MessageSender("user1", "User One", false);
        var sender2 = new MessageSender("user2", "User Two", false);

        var message1 = new Message(
            channel.Id,
            _tenantId,
            "M001",
            sender1,
            MessageType.Text,
            "Hello",
            DateTime.UtcNow
        );

        var message2 = new Message(
            channel.Id,
            _tenantId,
            "M002",
            sender2,
            MessageType.Text,
            "World",
            DateTime.UtcNow
        );

        _context.Messages.Add(message1);
        _context.Messages.Add(message2);

        // Create a message for a different tenant
        var otherTenantId = Guid.NewGuid();
        var otherSender = new MessageSender("user3", "User Three", false);
        var otherMessage = new Message(
            Guid.NewGuid(),
            otherTenantId,
            "M003",
            otherSender,
            MessageType.Text,
            "Other tenant message",
            DateTime.UtcNow
        );
        _context.Messages.Add(otherMessage);

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Query should be filtered by tenant
        var messages = await _context.Messages.ToListAsync(TestContext.Current.CancellationToken);

        // Assert - Should only return messages for the current tenant
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal(_tenantId, m.TenantId));
        Assert.DoesNotContain(messages, m => m.PlatformMessageId == "M003");
    }

    [Fact]
    public async Task ChannelFilter_WorksWithWorkspaceRelationship()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant1 = new Tenant("Tenant 1", "tenant-1", "free", 30);
        typeof(Entity).GetProperty("Id")!.SetValue(tenant1, _tenantId);
        _context.Tenants.Add(tenant1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var workspace1 = tenant1.AddWorkspace("Workspace 1", "Slack");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var channel1 = workspace1.AddChannel("general", "C001");
        var channel2 = workspace1.AddChannel("random", "C002");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Query channels through workspace
        var workspace = await _context.Workspaces
            .Include(w => w.Channels)
            .FirstOrDefaultAsync(w => w.Id == workspace1.Id, TestContext.Current.CancellationToken);

        // Assert - Channels should be accessible through workspace
        Assert.NotNull(workspace);
        Assert.Equal(2, workspace.Channels.Count);
        Assert.Contains(workspace.Channels, c => c.Name == "general");
        Assert.Contains(workspace.Channels, c => c.Name == "random");
    }

    [Fact]
    public void EntityConfigurations_ApplyCorrectly()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        // Act & Assert - Check that all entity configurations are applied
        var model = _context.Model;

        // Tenant configuration
        var tenantEntity = model.FindEntityType(typeof(Tenant));
        Assert.NotNull(tenantEntity);
        var slugProperty = tenantEntity!.FindProperty("Slug");
        Assert.NotNull(slugProperty);

        // Workspace configuration
        var workspaceEntity = model.FindEntityType(typeof(Workspace));
        Assert.NotNull(workspaceEntity);
        var tenantIdProperty = workspaceEntity!.FindProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
        var platformProperty = workspaceEntity!.FindProperty("Platform");
        Assert.NotNull(platformProperty);

        // Channel configuration
        var channelEntity = model.FindEntityType(typeof(Channel));
        Assert.NotNull(channelEntity);
        var workspaceIdProperty = channelEntity!.FindProperty("WorkspaceId");
        Assert.NotNull(workspaceIdProperty);

        // Message configuration
        var messageEntity = model.FindEntityType(typeof(Message));
        Assert.NotNull(messageEntity);
        var messageTenantIdProperty = messageEntity!.FindProperty("TenantId");
        Assert.NotNull(messageTenantIdProperty);
        var platformMessageIdProperty = messageEntity!.FindProperty("PlatformMessageId");
        Assert.NotNull(platformMessageIdProperty);
    }

    [Fact]
    public async Task DateTimeConversions_StoreAsUtc()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        typeof(Entity).GetProperty("Id")!.SetValue(tenant, _tenantId);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var workspace = tenant.AddWorkspace("Test Workspace", "Slack");
        var channel = workspace.AddChannel("general", "C123");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var messageTime = DateTime.UtcNow;  // Use UTC time directly
        var sender = new MessageSender("user1", "User One", false);
        var message = new Message(
            channel.Id,
            _tenantId,
            "M001",
            sender,
            MessageType.Text,
            "Test message",
            messageTime
        );
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Reload from database
        _context.ChangeTracker.Clear();
        var savedMessage = await _context.Messages
            .FirstOrDefaultAsync(m => m.PlatformMessageId == "M001", TestContext.Current.CancellationToken);

        // Assert - Message should be saved and retrieved with timestamp intact
        Assert.NotNull(savedMessage);
        Assert.Equal(messageTime.ToUniversalTime(), savedMessage.TimestampUtc.ToUniversalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CanConnectAsync_WithValidConnection_ShouldReturnTrue()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        // Act
        var canConnect = await _context.CanConnectAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test", "test", "free", 30);
        _context.Tenants.Add(tenant);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _context.SaveChangesAsync(cts.Token));
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntityTypes_ShouldSaveAll()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        var workspace = tenant.AddWorkspace("Test Workspace", "Test");
        var channel = workspace.AddChannel("Test Channel", "C001");

        _context.Tenants.Add(tenant);

        // Act
        var changes = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, changes); // Tenant + Workspace + Channel

        // Verify all entities are saved
        Assert.NotNull(await _context.Tenants.FindAsync(new object[] { tenant.Id }, TestContext.Current.CancellationToken));
        Assert.NotNull(await _context.Workspaces.FindAsync(new object[] { workspace.Id }, TestContext.Current.CancellationToken));
        Assert.NotNull(await _context.Channels.FindAsync(new object[] { channel.Id }, TestContext.Current.CancellationToken));
    }

    [Fact(Skip = "In-memory database doesn't enforce unique constraints")]
    public async Task SaveChangesAsync_WithFailure_ShouldRollbackChanges()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant1 = new Tenant("Tenant1", "tenant-1", "free", 30);
        var tenant2 = new Tenant("Tenant2", "tenant-1", "free", 30); // Duplicate slug

        _context.Tenants.Add(tenant1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.Tenants.Add(tenant2);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Verify first tenant still exists
        var existingTenant = await _context.Tenants.FindAsync(new object[] { tenant1.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(existingTenant);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ShouldReturnZero()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        // Act
        var changes = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, changes);
    }

    [Fact(Skip = "In-memory database doesn't enforce unique constraints")]
    public async Task ModelConfiguration_ShouldEnforceTenantIndexes()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var duplicateSlug = $"test-slug-{Guid.NewGuid()}";
        var tenant1 = new Tenant("Test1", duplicateSlug, "free", 30);
        var tenant2 = new Tenant("Test2", duplicateSlug, "free", 30); // Same slug

        _context.Tenants.Add(tenant1);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        _context.Tenants.Add(tenant2);

        // Act & Assert - Should throw due to unique constraint on Slug
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task WorkspaceChannelRelationship_ShouldCascadeDelete()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test", "test", "free", 30);
        var workspace = tenant.AddWorkspace("Workspace", "Test");
        var channel = workspace.AddChannel("Channel", "C001");

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Remove workspace
        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Channel should also be deleted
        var remainingChannels = await _context.Channels
            .Where(c => c.WorkspaceId == workspace.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Empty(remainingChannels);
    }

    [Fact]
    public async Task EntityTracking_ShouldTrackStateChangesCorrectly()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.TenantContext = _tenantContextMock.Object;
        _context.Database.EnsureCreated();

        var tenant = new Tenant("Test", "test", "free", 30);

        // Act & Assert - Added state
        _context.Tenants.Add(tenant);
        Assert.Equal(EntityState.Added, _context.Entry(tenant).State);

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EntityState.Unchanged, _context.Entry(tenant).State);

        // Modify
        tenant.UpdatePlan("professional", 60);
        Assert.Equal(EntityState.Modified, _context.Entry(tenant).State);

        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EntityState.Unchanged, _context.Entry(tenant).State);

        // Delete
        _context.Tenants.Remove(tenant);
        Assert.Equal(EntityState.Deleted, _context.Entry(tenant).State);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ShouldSetTimestamps()
    {
        // Arrange
        _context = new SigmaDbContext(_options);
        _context.Database.EnsureCreated();

        var tenantId = Guid.NewGuid();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);
        _context.TenantContext = tenantContext.Object;

        var tenant = new Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid()}", "free", 30);

        var workspace1 = new Workspace(tenantId, "Workspace 1", "slack");
        var workspace2 = new Workspace(tenantId, "Workspace 2", "discord");

        _context.Tenants.Add(tenant);
        _context.Workspaces.AddRange(workspace1, workspace2);

        // Act
        var result = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result);
        Assert.NotEqual(DateTime.MinValue, tenant.CreatedAtUtc);
        Assert.NotEqual(DateTime.MinValue, workspace1.CreatedAtUtc);
        Assert.NotEqual(DateTime.MinValue, workspace2.CreatedAtUtc);
        Assert.Null(tenant.UpdatedAtUtc);
        Assert.Null(workspace1.UpdatedAtUtc);
        Assert.Null(workspace2.UpdatedAtUtc);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

public class SigmaDbContextTimestampTests : IDisposable
{
    private readonly DbContextOptions<SigmaDbContext> _options;
    private SigmaDbContext _context = null!;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SigmaDbContextTimestampTests()
    {
        _options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);
        _tenantContextMock.Setup(x => x.TenantSlug).Returns((string?)null);
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedEntity_ShouldSetUpdatedTimestamp()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContext.Object;

        var tenant = new Tenant("Original Name", "original-slug", "free", 30);

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalCreatedAt = tenant.CreatedAtUtc;

        // Act
        // Can't directly modify Name as it's read-only, would need a method on Tenant
        // For this test, we'll just trigger the UpdatedAtUtc update
        context.Entry(tenant).State = EntityState.Modified;
        var result = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result);
        Assert.Equal(originalCreatedAt, tenant.CreatedAtUtc);
        Assert.NotNull(tenant.UpdatedAtUtc);
        Assert.True(tenant.UpdatedAtUtc > tenant.CreatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDeletedEntity_ShouldRemoveFromDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContext.Object;

        var workspace = new Workspace(tenantId, "To Delete", "slack");
        workspace.UpdateExternalId("W789");

        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        context.Workspaces.Remove(workspace);
        var result = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result);
        Assert.Null(await context.Workspaces.FindAsync(workspaceId));
    }

    [Fact]
    public async Task CanConnectAsync_WithValidDatabase_ShouldReturnTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SigmaDbContext(options);

        // Act
        var result = await context.CanConnectAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanConnectAsync_WhenExceptionOccurs_ShouldReturnFalse()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestSigmaDbContext(options);
        context.ShouldThrowOnConnect = true;

        // Act
        var result = await context.CanConnectAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_WithOnlyOptions_ShouldInitializeWithNullTenantContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new SigmaDbContext(options);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Tenants);
        Assert.NotNull(context.Workspaces);
        Assert.NotNull(context.Channels);
        Assert.NotNull(context.Messages);
    }

    [Fact]
    public async Task SaveChangesAsync_WithUnchangedEntity_ShouldNotUpdateTimestamps()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContext.Object;

        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalCreatedAt = tenant.CreatedAtUtc;
        var originalUpdatedAt = tenant.UpdatedAtUtc;

        // Act - Save again without changes
        var result = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
        Assert.Equal(originalCreatedAt, tenant.CreatedAtUtc);
        Assert.Equal(originalUpdatedAt, tenant.UpdatedAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMixedOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new SigmaDbContext(options);
        context.TenantContext = tenantContext.Object;

        // Add initial data
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);

        var workspace1 = new Workspace(tenantId, "Workspace 1", "slack");
        workspace1.UpdateExternalId("W1");

        var workspace2 = new Workspace(tenantId, "Workspace 2", "discord");
        workspace2.UpdateExternalId("W2");

        context.Tenants.Add(tenant);
        context.Workspaces.AddRange(workspace1, workspace2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Mixed operations
        context.Entry(workspace1).State = EntityState.Modified; // Mark as modified
        context.Workspaces.Remove(workspace2); // Delete
        var workspace3 = new Workspace(tenantId, "Workspace 3", "telegram"); // Add new
        workspace3.UpdateExternalId("W3");
        context.Workspaces.Add(workspace3);

        var result = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result); // 1 modified, 1 deleted, 1 added
        Assert.NotNull(workspace1.UpdatedAtUtc);
        Assert.NotNull(workspace3.CreatedAtUtc);
        Assert.Null(await context.Workspaces.FindAsync(workspace2.Id));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}