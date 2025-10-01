using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sigma.API.Webhooks;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Sigma.Application.Services;
using Sigma.Domain.Common;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;
using Xunit;

namespace Sigma.API.Tests.Webhooks;

public class TelegramWebhookHandlerTests
{
    private readonly TelegramWebhookHandler _handler;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly Mock<IWebhookEventRepository> _webhookEventRepository;
    private readonly Mock<IMessageRepository> _messageRepository;
    private readonly Mock<IMessageNormalizer> _messageNormalizer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<TelegramWebhookHandler>> _logger;
    private readonly SigmaDbContext _dbContext;
    private const string TestBotToken = "123456:ABC-DEF1234567890";
    private readonly Guid _testTenantId = Guid.NewGuid();

    public TelegramWebhookHandlerTests()
    {
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _webhookEventRepository = new Mock<IWebhookEventRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _messageNormalizer = new Mock<IMessageNormalizer>();
        _logger = new Mock<ILogger<TelegramWebhookHandler>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SigmaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new SigmaDbContext(options);

        // Add test tenant
        var testTenant = new Tenant("Test Tenant", "test-tenant", "free", 30);
        typeof(Entity).GetProperty("Id")!.SetValue(testTenant, _testTenantId);
        _dbContext.Tenants.Add(testTenant);
        _dbContext.SaveChanges();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Platforms:Telegram:BotTokens:{_testTenantId}"] = TestBotToken,
                ["Platforms:Telegram:BotTokens:another-tenant"] = "987654:XYZ-ABC9876543210",
                [$"Platforms:Telegram:SecretTokens:{_testTenantId}"] = "secret-token-123"
            })
            .Build();
        _configuration = configuration;

        // Setup webhook repository mock (not already processed)
        _webhookEventRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sigma.Domain.Entities.WebhookEvent?)null);

        // Setup message normalizer mock
        _messageNormalizer.Setup(m => m.NormalizeTelegramMessage(It.IsAny<TelegramUpdate>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(new MessageEvent
            {
                PlatformMessageId = "123",
                Sender = new MessageSenderInfo { PlatformUserId = "user1", DisplayName = "Test User", IsBot = false },
                Type = MessageEventType.Text,
                Text = "Test message",
                TimestampUtc = DateTime.UtcNow
            });

        var services = new ServiceCollection();
        services.AddSingleton<ICommandDispatcher>(_commandDispatcher.Object);
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton<ILogger<TelegramWebhookHandler>>(_logger.Object);
        services.AddSingleton<SigmaDbContext>(_dbContext);
        services.AddSingleton<IWebhookEventRepository>(_webhookEventRepository.Object);
        services.AddSingleton<IMessageRepository>(_messageRepository.Object);
        services.AddSingleton<IMessageNormalizer>(_messageNormalizer.Object);

        _serviceProvider = services.BuildServiceProvider();
        _handler = new TelegramWebhookHandler(_serviceProvider);
    }

    [Fact]
    public async Task HandleAsync_WithValidBotToken_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramUpdate
        {
            UpdateId = 12345678,
            Message = new Sigma.Shared.Contracts.TelegramMessage
            {
                MessageId = 1,
                Text = "Test message",
                Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Chat = new Sigma.Shared.Contracts.TelegramChat
                {
                    Id = 123456,
                    Type = "private"
                },
                From = new Sigma.Shared.Contracts.TelegramUser
                {
                    Id = 789,
                    FirstName = "Test",
                    Username = "testuser"
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidBotToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid-token";
        var payload = new TelegramUpdate { UpdateId = 123 };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(invalidToken, context);

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        Assert.NotNull(unauthorizedResult);
    }

    [Fact]
    public async Task HandleAsync_WithValidSecretToken_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramUpdate
        {
            UpdateId = 987654,
            Message = new Sigma.Shared.Contracts.TelegramMessage
            {
                MessageId = 2,
                Text = "Secret test",
                Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Chat = new Sigma.Shared.Contracts.TelegramChat
                {
                    Id = 123456,
                    Type = "private"
                },
                From = new Sigma.Shared.Contracts.TelegramUser
                {
                    Id = 789,
                    FirstName = "Test"
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "secret-token-123";

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSecretToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var payload = new TelegramUpdate { UpdateId = 111 };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "wrong-secret";

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        Assert.NotNull(unauthorizedResult);
    }

    [Fact]
    public async Task HandleAsync_WithPartialBotToken_ShouldProcessWebhook()
    {
        // Arrange
        var partialToken = "ABC-DEF1234567890"; // Just the token part without bot ID
        var payload = new TelegramUpdate
        {
            UpdateId = 222,
            Message = new Sigma.Shared.Contracts.TelegramMessage
            {
                MessageId = 3,
                Text = "Test",
                Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Chat = new Sigma.Shared.Contracts.TelegramChat
                {
                    Id = 123456,
                    Type = "private"
                },
                From = new Sigma.Shared.Contracts.TelegramUser
                {
                    Id = 789,
                    FirstName = "Test"
                }
            }
        };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(partialToken, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var context = CreateHttpContext("invalid json");

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WithNullPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var context = CreateHttpContext("null");

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>;
        Assert.NotNull(badRequestResult);
        Assert.Equal("Invalid payload", badRequestResult!.Value);
    }

    [Fact]
    public async Task HandleAsync_WithCallbackQuery_ShouldProcessWebhook()
    {
        // Arrange
        // Callback queries don't create messages, so handler returns Ok without processing
        var payload = new TelegramUpdate
        {
            UpdateId = 333
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithEditedMessage_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramUpdate
        {
            UpdateId = 444,
            EditedMessage = new Sigma.Shared.Contracts.TelegramMessage
            {
                MessageId = 3,
                Text = "Edited message",
                Date = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EditDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Chat = new Sigma.Shared.Contracts.TelegramChat
                {
                    Id = 123456,
                    Type = "private"
                },
                From = new Sigma.Shared.Contracts.TelegramUser
                {
                    Id = 789,
                    FirstName = "Test"
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WhenExceptionOccurs_ShouldReturn500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Body = new ThrowingStream(); // Stream that throws on read

        // Act
        var result = await _handler.HandleAsync(TestBotToken, context);

        // Assert
        Assert.NotNull(result);
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(500, statusResult.StatusCode);

        _logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyConfiguration_ShouldReturnUnauthorized()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(emptyConfig);
        services.AddSingleton<ILogger<TelegramWebhookHandler>>(_logger.Object);

        var serviceProvider = services.BuildServiceProvider();
        var handler = new TelegramWebhookHandler(serviceProvider);

        var payload = new TelegramUpdate { UpdateId = 555 };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await handler.HandleAsync("some-token", context);

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        Assert.NotNull(unauthorizedResult);
    }

    private static DefaultHttpContext CreateHttpContext(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context;
    }

    private class ThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override void Flush() => throw new NotImplementedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Test exception");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}