using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence;
using Sigma.Infrastructure.Persistence.Repositories;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Infrastructure.Tests.Repositories;

public class WorkspaceRepositoryTests : IDisposable
{
    private readonly SigmaDbContext _context;
    private readonly WorkspaceRepository _repository;
    private readonly Guid _tenantId;
    private readonly Tenant _tenant;

    public WorkspaceRepositoryTests()
    {
        // Create a tenant first to get its ID
        _tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _tenantId = _tenant.Id;

        // Create context with the tenant's ID for proper filtering
        _context = TestDbContextFactory.CreateDbContext(_tenantId);
        _repository = new WorkspaceRepository(_context);

        // Add the tenant to the context
        _context.Tenants.Add(_tenant);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingWorkspace_ShouldReturnWorkspace()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.Slack);
        workspace.UpdateExternalId("T123456");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(workspace.Id, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workspace.Id, result.Id);
        Assert.Equal("Test Workspace", result.Name);
        Assert.Equal(Platform.Slack, result.Platform);
        Assert.Equal("T123456", result.ExternalId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentWorkspace_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithExistingWorkspaces_ShouldReturnAll()
    {
        // Arrange
        var workspace1 = _tenant.AddWorkspace("Workspace 1", Platform.Slack);
        workspace1.UpdateExternalId("T111");
        var workspace2 = _tenant.AddWorkspace("Workspace 2", Platform.Discord);
        workspace2.UpdateExternalId("D222");
        var workspace3 = _tenant.AddWorkspace("Workspace 3", Platform.Telegram);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByTenantIdAsync(_tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var workspaces = result.ToList();
        Assert.Equal(3, workspaces.Count);
        Assert.All(workspaces, w => Assert.Equal(_tenant.Id, w.TenantId));
        Assert.Contains(workspaces, w => w.Name == "Workspace 1");
        Assert.Contains(workspaces, w => w.Name == "Workspace 2");
        Assert.Contains(workspaces, w => w.Name == "Workspace 3");
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithNoWorkspaces_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByTenantIdAsync(_tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithDifferentTenant_ShouldNotReturnOtherTenantWorkspaces()
    {
        // Arrange
        var workspace1 = _tenant.AddWorkspace("Workspace 1", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var otherTenantId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByTenantIdAsync(otherTenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_WithValidWorkspace_ShouldAddToContext()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("New Workspace", Platform.WhatsApp);
        workspace.UpdateExternalId("W999");

        // Act
        await _repository.AddAsync(workspace, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedWorkspace = await _context.Workspaces.FindAsync(new object[] { workspace.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(savedWorkspace);
        Assert.Equal("New Workspace", savedWorkspace.Name);
        Assert.Equal(Platform.WhatsApp, savedWorkspace.Platform);
        Assert.Equal("W999", savedWorkspace.ExternalId);
        Assert.Equal(_tenant.Id, savedWorkspace.TenantId);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingWorkspace_ShouldUpdateInContext()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Original Name", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        workspace.Activate();
        await _repository.UpdateAsync(workspace, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var updatedWorkspace = await _context.Workspaces.FindAsync(new object[] { workspace.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updatedWorkspace);
        Assert.True(updatedWorkspace.IsActive);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithExistingWorkspace_ShouldReturnWorkspace()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.Slack);
        workspace.UpdateExternalId("UNIQUE123");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByExternalIdAsync("UNIQUE123", Platform.Slack, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workspace.Id, result.Id);
        Assert.Equal("UNIQUE123", result.ExternalId);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithNonExistentExternalId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByExternalIdAsync("NON_EXISTENT", Platform.Slack, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithNullExternalId_ShouldReturnNull()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.Telegram);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByExternalIdAsync(null!, Platform.Telegram, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingWorkspace_ShouldReturnTrue()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await _repository.ExistsAsync(workspace.Id, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentWorkspace_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid(), _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithWrongTenantId_ShouldReturnFalse()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await _repository.ExistsAsync(workspace.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithDifferentPlatform_ShouldReturnNull()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Test Workspace", Platform.WhatsApp);
        workspace.UpdateExternalId("ext-123");
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByExternalIdAsync("ext-123", Platform.Telegram, _tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTenantIdAsync_WithMultiplePlatforms_ShouldReturnAll()
    {
        // Arrange
        var workspace1 = _tenant.AddWorkspace("WS1", Platform.WhatsApp);
        var workspace2 = _tenant.AddWorkspace("WS2", Platform.Telegram);
        var workspace3 = _tenant.AddWorkspace("WS3", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByTenantIdAsync(_tenant.Id, TestContext.Current.CancellationToken);

        // Assert
        var workspaces = result.ToList();
        Assert.Equal(3, workspaces.Count);
        Assert.Contains(workspaces, w => w.Platform == Platform.WhatsApp);
        Assert.Contains(workspaces, w => w.Platform == Platform.Telegram);
        Assert.Contains(workspaces, w => w.Platform == Platform.Slack);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedProperties_ShouldUpdateAllFields()
    {
        // Arrange
        var workspace = _tenant.AddWorkspace("Original", Platform.Slack);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        workspace.UpdateExternalId("new-external");
        await _repository.UpdateAsync(workspace, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Assert
        var result = await _repository.GetByIdAsync(workspace.Id, _tenant.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Original", result.Name); // Name is read-only in domain
        Assert.Equal("new-external", result.ExternalId);
    }

    // NOTE: EF Core's AddAsync doesn't actually perform async work or check cancellation tokens immediately.
    // It's synchronous and only exists for interface consistency. Testing this would be testing EF Core's
    // implementation rather than our repository. Cancellation is properly handled during SaveChangesAsync.

    public void Dispose()
    {
        _context.Dispose();
    }
}