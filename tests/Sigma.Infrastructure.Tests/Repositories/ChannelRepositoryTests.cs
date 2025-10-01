using Sigma.Domain.Common;
using Sigma.Domain.Entities;
using Sigma.Infrastructure.Persistence.Repositories;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Infrastructure.Tests.Repositories;

public class ChannelRepositoryTests : IDisposable
{
    private readonly Infrastructure.Persistence.SigmaDbContext _context;
    private readonly ChannelRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ChannelRepositoryTests()
    {
        _context = TestDbContextFactory.CreateDbContext(_tenantId);
        _repository = new ChannelRepository(_context);

        // Setup test data
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        // Force the tenant ID to match the one we're using in tests
        typeof(Entity).GetProperty("Id")!.SetValue(tenant, _tenantId);
        _context.Tenants.Add(tenant);

        var workspace = new Workspace(_tenantId, "Test Workspace", Platform.Slack);
        workspace.UpdateExternalId("ext-ws-1");
        _context.Workspaces.Add(workspace);
        _workspaceId = workspace.Id;

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingChannel_ShouldReturnChannel()
    {
        // Arrange
        var channel = new Channel(_workspaceId, "Test Channel", "ext-ch-1");
        _context.Channels.Add(channel);
        _context.Entry(channel).Property("TenantId").CurrentValue = _tenantId;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(channel.Id, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(channel.Id, result.Id);
        Assert.Equal("Test Channel", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentChannel_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByWorkspaceIdAsync_WithChannels_ShouldReturnAll()
    {
        // Arrange
        var channel1 = new Channel(_workspaceId, "Channel 1", "ext-ch-1");
        var channel2 = new Channel(_workspaceId, "Channel 2", "ext-ch-2");
        _context.Channels.AddRange(channel1, channel2);
        _context.Entry(channel1).Property("TenantId").CurrentValue = _tenantId;
        _context.Entry(channel2).Property("TenantId").CurrentValue = _tenantId;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByWorkspaceIdAsync(_workspaceId, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByExternalIdAsync_WithExistingChannel_ShouldReturnChannel()
    {
        // Arrange
        var channel = new Channel(_workspaceId, "Test Channel", "ext-ch-unique");
        _context.Channels.Add(channel);
        _context.Entry(channel).Property("TenantId").CurrentValue = _tenantId;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByExternalIdAsync("ext-ch-unique", _workspaceId, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ext-ch-unique", result.ExternalId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddChannel()
    {
        // Arrange
        var channel = new Channel(_workspaceId, "New Channel", "ext-ch-new");

        // Act
        await _repository.AddAsync(channel, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await _context.Channels.FindAsync(new object[] { channel.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("New Channel", result.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateChannel()
    {
        // Arrange
        var channel = new Channel(_workspaceId, "Original Name", "ext-ch-1");
        _context.Channels.Add(channel);
        _context.Entry(channel).Property("TenantId").CurrentValue = _tenantId;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        // Channel doesn't have UpdateName method, let's test deactivation instead
        channel.Deactivate();
        await _repository.UpdateAsync(channel, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await _context.Channels.FindAsync(new object[] { channel.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingChannel_ShouldReturnTrue()
    {
        // Arrange
        var channel = new Channel(_workspaceId, "Test Channel", "ext-ch-exists");
        _context.Channels.Add(channel);
        _context.Entry(channel).Property("TenantId").CurrentValue = _tenantId;
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.ExistsAsync(channel.Id, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentChannel_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid(), _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}