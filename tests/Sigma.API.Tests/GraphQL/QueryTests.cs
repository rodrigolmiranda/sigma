using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sigma.API.GraphQL;
using Sigma.Application.Contracts;
using Sigma.Application.Queries;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using Sigma.Domain.ValueObjects;
using Sigma.Shared.Enums;
using System.Linq;
using Xunit;

namespace Sigma.API.Tests.GraphQL;

public class QueryTests
{
    private readonly Query _query;
    private readonly Mock<ITenantRepository> _tenantRepository;
    private readonly Mock<IWorkspaceRepository> _workspaceRepository;
    private readonly Mock<IChannelRepository> _channelRepository;
    private readonly Mock<IMessageRepository> _messageRepository;
    private readonly Mock<IQueryDispatcher> _queryDispatcher;
    private readonly Mock<SigmaDbContext> _dbContext;

    public QueryTests()
    {
        _query = new Query();
        _tenantRepository = new Mock<ITenantRepository>();
        _workspaceRepository = new Mock<IWorkspaceRepository>();
        _channelRepository = new Mock<IChannelRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _queryDispatcher = new Mock<IQueryDispatcher>();

        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new Mock<SigmaDbContext>(options);
    }

    [Fact]
    public async Task GetTenants_ShouldReturnAllActiveTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant("Tenant 1", "tenant-1", "free", 30),
            new Tenant("Tenant 2", "tenant-2", "professional", 90),
            new Tenant("Tenant 3", "tenant-3", "enterprise", 365)
        };

        _tenantRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants.AsEnumerable());

        // Act
        var result = await _query.GetTenants(_tenantRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var tenantList = result.ToList();
        Assert.Equal(3, tenantList.Count);
        Assert.Contains(tenantList, t => t.Name == "Tenant 1");
        Assert.Contains(tenantList, t => t.Name == "Tenant 2");
        Assert.Contains(tenantList, t => t.Name == "Tenant 3");
        _tenantRepository.Verify(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenants_WithEmptyList_ShouldReturnEmptyQueryable()
    {
        // Arrange
        _tenantRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Tenant>());

        // Act
        var result = await _query.GetTenants(_tenantRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTenant_WithExistingId_ShouldReturnTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant("Test Tenant", $"test-tenant-{Guid.NewGuid():N}", "professional", 90);
        var queryResult = Result<Tenant>.Success(tenant);

        _queryDispatcher.Setup(d => d.DispatchAsync<GetTenantByIdQuery, Tenant>(
                It.Is<GetTenantByIdQuery>(q => q.Id == tenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        // Act
        var result = await _query.GetTenant(tenantId, _queryDispatcher.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Tenant", result.Name);
        _queryDispatcher.Verify(d => d.DispatchAsync<GetTenantByIdQuery, Tenant>(
            It.IsAny<GetTenantByIdQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenant_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var queryResult = Result<Tenant>.Failure(new Error("NOT_FOUND", "Tenant not found"));

        _queryDispatcher.Setup(d => d.DispatchAsync<GetTenantByIdQuery, Tenant>(
                It.Is<GetTenantByIdQuery>(q => q.Id == tenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        // Act
        var result = await _query.GetTenant(tenantId, _queryDispatcher.Object, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkspaces_ShouldReturnWorkspacesForTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaces = new List<Workspace>
        {
            new Workspace(tenantId, "Workspace 1", Platform.Slack),
            new Workspace(tenantId, "Workspace 2", Platform.Discord)
        };

        _workspaceRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaces.AsEnumerable());

        // Act
        var result = await _query.GetWorkspaces(tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var workspaceList = result.ToList();
        Assert.Equal(2, workspaceList.Count);
        Assert.Contains(workspaceList, w => w.Name == "Workspace 1");
        Assert.Contains(workspaceList, w => w.Name == "Workspace 2");
        _workspaceRepository.Verify(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkspace_WithExistingId_ShouldReturnWorkspace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var workspace = new Workspace(tenantId, "Test Workspace", Platform.Telegram);

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        // Act
        var result = await _query.GetWorkspace(workspaceId, tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Workspace", result.Name);
        Assert.Equal(Platform.Telegram, result.Platform);
        _workspaceRepository.Verify(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkspace_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _workspaceRepository.Setup(r => r.GetByIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        // Act
        var result = await _query.GetWorkspace(workspaceId, tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetChannels_ShouldReturnChannelsForWorkspace()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var channels = new List<Channel>
        {
            new Channel(workspaceId, "general", "channel-1"),
            new Channel(workspaceId, "random", "channel-2"),
            new Channel(workspaceId, "dev-team", "channel-3")
        };

        _channelRepository.Setup(r => r.GetByWorkspaceIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels.AsEnumerable());

        // Act
        var result = await _query.GetChannels(workspaceId, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var channelList = result.ToList();
        Assert.Equal(3, channelList.Count);
        Assert.Contains(channelList, c => c.Name == "general");
        Assert.Contains(channelList, c => c.Name == "random");
        Assert.Contains(channelList, c => c.Name == "dev-team");
        _channelRepository.Verify(r => r.GetByWorkspaceIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChannel_WithExistingId_ShouldReturnChannel()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var channel = new Channel(workspaceId, "test-channel", "ch-123");

        _channelRepository.Setup(r => r.GetByIdAsync(channelId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        // Act
        var result = await _query.GetChannel(channelId, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-channel", result.Name);
        _channelRepository.Verify(r => r.GetByIdAsync(channelId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnMessagesForChannel()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var messages = new List<Message>
        {
            new Message(channelId, tenantId, "msg-1", new MessageSender("user-1", "User One", false), Sigma.Domain.ValueObjects.MessageType.Text, "Hello", DateTime.UtcNow),
            new Message(channelId, tenantId, "msg-2", new MessageSender("user-2", "User Two", false), Sigma.Domain.ValueObjects.MessageType.Text, "Hi there", DateTime.UtcNow),
            new Message(channelId, tenantId, "msg-3", new MessageSender("user-1", "User One", false), Sigma.Domain.ValueObjects.MessageType.Text, "How are you?", DateTime.UtcNow)
        };

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages.AsEnumerable());

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var messageList = result.ToList();
        Assert.Equal(3, messageList.Count);
        _messageRepository.Verify(r => r.GetByChannelIdAsync(channelId, tenantId, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessages_WithCustomLimit_ShouldUseProvidedLimit()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var limit = 50;
        var messages = new List<Message>();

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages.AsEnumerable());

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None, limit);

        // Assert
        Assert.NotNull(result);
        _messageRepository.Verify(r => r.GetByChannelIdAsync(channelId, tenantId, limit, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessage_WithExistingId_ShouldReturnMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var message = new Message(channelId, tenantId, "msg-123", new MessageSender("user-456", "Test User", false), Sigma.Domain.ValueObjects.MessageType.Text, "Test message content", DateTime.UtcNow);

        _messageRepository.Setup(r => r.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _query.GetMessage(messageId, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test message content", result.Text);
        _messageRepository.Verify(r => r.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetVersion_ShouldReturnVersionString()
    {
        // Act
        var version = _query.GetVersion();

        // Assert
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public async Task GetHealthStatus_WithConnectableDatabase_ShouldReturnTrue()
    {
        // Arrange
        _dbContext.Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _query.GetHealthStatus(_dbContext.Object, CancellationToken.None);

        // Assert
        Assert.True(result);
        _dbContext.Verify(db => db.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthStatus_WithFailedDatabase_ShouldReturnFalse()
    {
        // Arrange
        _dbContext.Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _query.GetHealthStatus(_dbContext.Object, CancellationToken.None);

        // Assert
        Assert.False(result);
        _dbContext.Verify(db => db.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHealthStatus_WithDatabaseException_ShouldThrow()
    {
        // Arrange
        _dbContext.Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _query.GetHealthStatus(_dbContext.Object, CancellationToken.None));
    }

    [Fact]
    public async Task AllMethods_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _tenantRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                return await Task.FromResult<IEnumerable<Tenant>>(new List<Tenant>());
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _query.GetTenants(_tenantRepository.Object, cts.Token));
    }

    [Fact]
    public async Task GetTenant_WithFailedQuery_ShouldReturnNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var queryResult = Result<Tenant>.Failure(new Error("NOT_FOUND", "Tenant not found"));

        _queryDispatcher.Setup(d => d.DispatchAsync<GetTenantByIdQuery, Tenant>(
                It.Is<GetTenantByIdQuery>(q => q.Id == tenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryResult);

        // Act
        var result = await _query.GetTenant(tenantId, _queryDispatcher.Object, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _queryDispatcher.Verify(d => d.DispatchAsync<GetTenantByIdQuery, Tenant>(
            It.IsAny<GetTenantByIdQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkspaces_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _workspaceRepository.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _query.GetWorkspaces(tenantId, _workspaceRepository.Object, cts.Token));
    }

    [Fact]
    public async Task GetWorkspace_WithNullResult_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _workspaceRepository.Setup(r => r.GetByIdAsync(id, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        // Act
        var result = await _query.GetWorkspace(id, tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _workspaceRepository.Verify(r => r.GetByIdAsync(id, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChannels_WithMultipleChannels_ShouldReturnAll()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var channels = new List<Channel>
        {
            new Channel(workspaceId, "general", "C001"),
            new Channel(workspaceId, "random", "C002"),
            new Channel(workspaceId, "announcements", "C003")
        };

        _channelRepository.Setup(r => r.GetByWorkspaceIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels.AsEnumerable());

        // Act
        var result = await _query.GetChannels(workspaceId, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var channelList = result.ToList();
        Assert.Equal(3, channelList.Count);
        Assert.Contains(channelList, c => c.Name == "general");
        Assert.Contains(channelList, c => c.Name == "random");
        Assert.Contains(channelList, c => c.Name == "announcements");
    }

    [Fact]
    public async Task GetChannel_WithValidId_ShouldReturnChannel()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var channel = new Channel(Guid.NewGuid(), "test-channel", "C123");

        _channelRepository.Setup(r => r.GetByIdAsync(id, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);

        // Act
        var result = await _query.GetChannel(id, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result);
        Assert.Equal("test-channel", result.Name);
    }

    [Fact]
    public async Task GetMessages_WithCustomLimit_ShouldRespectLimit()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var limit = 50;
        var messages = Enumerable.Range(1, 50).Select(i => new Message(
            channelId,
            tenantId,
            $"msg-{i}",
            new MessageSender($"U{i}", $"User {i}", false),
            Sigma.Domain.ValueObjects.MessageType.Text,
            $"Message {i}",
            DateTime.UtcNow.AddMinutes(-i)));

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None, limit);

        // Assert
        Assert.NotNull(result);
        var messageList = result.ToList();
        Assert.Equal(50, messageList.Count);
        _messageRepository.Verify(r => r.GetByChannelIdAsync(channelId, tenantId, limit, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessages_WithDefaultLimit_ShouldUse100()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var messages = Enumerable.Range(1, 100).Select(i => new Message(
            channelId,
            tenantId,
            $"msg-{i}",
            new MessageSender($"U{i}", $"User {i}", false),
            Sigma.Domain.ValueObjects.MessageType.Text,
            $"Message {i}",
            DateTime.UtcNow.AddMinutes(-i)));

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var messageList = result.ToList();
        Assert.Equal(100, messageList.Count);
        _messageRepository.Verify(r => r.GetByChannelIdAsync(channelId, tenantId, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessage_WithExistingMessage_ShouldReturnIt()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var channelId = Guid.NewGuid();
        var message = new Message(
            channelId,
            tenantId,
            "msg-12345",
            new MessageSender("U789", "Test User", false),
            Sigma.Domain.ValueObjects.MessageType.Text,
            "Test message",
            DateTime.UtcNow);
        var messageId = message.Id; // Get the actual message ID

        _messageRepository.Setup(r => r.GetByIdAsync(messageId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        // Act
        var result = await _query.GetMessage(messageId, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(messageId, result.Id);
        Assert.Equal("Test message", result.Text);
        Assert.Equal(tenantId, result.TenantId);
    }

    [Fact]
    public async Task GetMessage_WithNonExistingMessage_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _messageRepository.Setup(r => r.GetByIdAsync(id, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _query.GetMessage(id, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetVersion_ShouldReturnCorrectVersion()
    {
        // Act
        var version = _query.GetVersion();

        // Assert
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public async Task GetHealthStatus_WithWorkingDatabase_ShouldReturnTrue()
    {
        // Arrange
        _dbContext.Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _query.GetHealthStatus(_dbContext.Object, CancellationToken.None);

        // Assert
        Assert.True(result);
        _dbContext.Verify(db => db.CanConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkspaces_WithEmptyResult_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _workspaceRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Workspace>());

        // Act
        var result = await _query.GetWorkspaces(tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChannels_WithEmptyResult_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _channelRepository.Setup(r => r.GetByWorkspaceIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Channel>());

        // Act
        var result = await _query.GetChannels(workspaceId, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMessages_WithEmptyChannel_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Message>());

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTenants_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(
            async () => await _query.GetTenants(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetWorkspaces_WithLargeTenantData_ShouldHandleCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var workspaces = new List<Workspace>();
        for (int i = 0; i < 100; i++)
        {
            workspaces.Add(new Workspace(tenantId, $"Workspace {i}", Platform.Slack));
        }

        _workspaceRepository.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspaces);

        // Act
        var result = await _query.GetWorkspaces(tenantId, _workspaceRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Count());
    }

    [Fact]
    public async Task GetChannels_WithLargeWorkspace_ShouldHandleCorrectly()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var workspace = new Workspace(tenantId, "Large Workspace", Platform.Discord);
        var channels = new List<Channel>();

        for (int i = 0; i < 50; i++)
        {
            channels.Add(workspace.AddChannel($"channel-{i}", $"CH{i}"));
        }

        _channelRepository.Setup(r => r.GetByWorkspaceIdAsync(workspaceId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(channels);

        // Act
        var result = await _query.GetChannels(workspaceId, tenantId, _channelRepository.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Count());
    }

    [Fact]
    public async Task GetMessages_WithSpecificLimit_ShouldUseCorrectLimit()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var customLimit = 50;
        var messages = new List<Message>();

        for (int i = 0; i < customLimit; i++)
        {
            messages.Add(new Message(
                channelId,
                tenantId,
                $"msg-{i}",
                new MessageSender($"user-{i}", $"User {i}", false),
                Sigma.Domain.ValueObjects.MessageType.Text,
                $"Message {i}",
                DateTime.UtcNow.AddMinutes(-i)));
        }

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, customLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None, customLimit);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customLimit, result.Count());
        _messageRepository.Verify(r => r.GetByChannelIdAsync(channelId, tenantId, customLimit, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task GetMessages_WithVariousLimits_ShouldRespectLimit(int limit)
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var messages = new List<Message>();

        for (int i = 0; i < Math.Min(limit, 100); i++)
        {
            messages.Add(new Message(
                channelId,
                tenantId,
                $"msg-{i}",
                new MessageSender($"user-{i}", $"User {i}", false),
                Sigma.Domain.ValueObjects.MessageType.Text,
                $"Message {i}",
                DateTime.UtcNow));
        }

        _messageRepository.Setup(r => r.GetByChannelIdAsync(channelId, tenantId, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        // Act
        var result = await _query.GetMessages(channelId, tenantId, _messageRepository.Object, CancellationToken.None, limit);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Math.Min(limit, 100), result.Count());
    }
}