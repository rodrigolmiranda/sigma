using Sigma.Domain.Entities;
using Sigma.Domain.ValueObjects;
using Sigma.Infrastructure.Persistence.Repositories;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.Infrastructure.Tests.Repositories;

public class MessageRepositoryTests : IDisposable
{
    private readonly Infrastructure.Persistence.SigmaDbContext _context;
    private readonly MessageRepository _repository;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _channelId = Guid.NewGuid();

    public MessageRepositoryTests()
    {
        _context = TestDbContextFactory.CreateDbContext(_tenantId);
        _repository = new MessageRepository(_context);

        // Setup test data
        var tenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        _context.Tenants.Add(tenant);

        var workspace = new Workspace(tenant.Id, "Test Workspace", Platform.Slack);
        workspace.UpdateExternalId("ext-ws-1");
        _context.Workspaces.Add(workspace);

        var channel = new Channel(workspace.Id, "Test Channel", "ext-ch-1");
        _context.Channels.Add(channel);
        _channelId = channel.Id;

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-1", sender, MessageType.Text, "Test message", DateTime.UtcNow);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(message.Id, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
        Assert.Equal("Test message", result.Text);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentMessage_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByChannelIdAsync_WithMessages_ShouldReturnAll()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message1 = new Message(_channelId, _tenantId, "ext-msg-1", sender, MessageType.Text, "Message 1", DateTime.UtcNow);
        var message2 = new Message(_channelId, _tenantId, "ext-msg-2", sender, MessageType.Text, "Message 2", DateTime.UtcNow);
        _context.Messages.AddRange(message1, message2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByChannelIdAsync(_channelId, _tenantId, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByChannelIdAsync_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        for (int i = 0; i < 15; i++)
        {
            var message = new Message(_channelId, _tenantId, $"ext-msg-{i}", sender, MessageType.Text, $"Message {i}", DateTime.UtcNow);
            _context.Messages.Add(message);
        }
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByChannelIdAsync(_channelId, _tenantId, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Count());
    }

    [Fact]
    public async Task GetByPlatformIdAsync_WithExistingMessage_ShouldReturnMessage()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-unique", sender, MessageType.Text, "Test message", DateTime.UtcNow);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByPlatformIdAsync("ext-msg-unique", _channelId, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ext-msg-unique", result.PlatformMessageId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMessage()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-new", sender, MessageType.Text, "New message", DateTime.UtcNow);

        // Act
        await _repository.AddAsync(message, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await _context.Messages.FindAsync(new object[] { message.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("New message", result.Text);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateMessage()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-1", sender, MessageType.Text, "Original", DateTime.UtcNow);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        message.MarkAsEdited("Updated content", DateTime.UtcNow);
        await _repository.UpdateAsync(message, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await _context.Messages.FindAsync(new object[] { message.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("Updated content", result.Text);
        Assert.NotNull(result.EditedAtUtc);
    }

    [Fact]
    public async Task MarkAsDeleted_ShouldSoftDeleteMessage()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-del", sender, MessageType.Text, "To delete", DateTime.UtcNow);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        message.MarkAsDeleted();
        await _repository.UpdateAsync(message, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var result = await _context.Messages.FindAsync(new object[] { message.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task GetRecentAsync_WithMatchingTimestamp_ShouldReturnMessages()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var now = DateTime.UtcNow;
        var message1 = new Message(_channelId, _tenantId, "ext-msg-1", sender, MessageType.Text, "Old message", now.AddHours(-2));
        var message2 = new Message(_channelId, _tenantId, "ext-msg-2", sender, MessageType.Text, "Recent message", now.AddMinutes(-30));
        var message3 = new Message(_channelId, _tenantId, "ext-msg-3", sender, MessageType.Text, "Very recent", now.AddMinutes(-5));
        _context.Messages.AddRange(message1, message2, message3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetRecentAsync(_channelId, _tenantId, now.AddHours(-1), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, m => Assert.True(m.TimestampUtc >= now.AddHours(-1)));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingMessage_ShouldReturnTrue()
    {
        // Arrange
        var sender = new MessageSender("ext-user-1", "Test User", false);
        var message = new Message(_channelId, _tenantId, "ext-msg-exists", sender, MessageType.Text, "Existing message", DateTime.UtcNow);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.ExistsAsync("ext-msg-exists", _channelId, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentMessage_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("non-existent", _channelId, _tenantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}