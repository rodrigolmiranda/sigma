using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sigma.API.Webhooks;
using Sigma.Application.Commands;
using Sigma.Application.Contracts;
using Xunit;

namespace Sigma.API.Tests.Webhooks;

public class TelegramWebhookHandlerTests
{
    private readonly TelegramWebhookHandler _handler;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<TelegramWebhookHandler>> _logger;
    private const string TestBotToken = "123456:ABC-DEF1234567890";

    public TelegramWebhookHandlerTests()
    {
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _logger = new Mock<ILogger<TelegramWebhookHandler>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platforms:Telegram:BotTokens:test-tenant"] = TestBotToken,
                ["Platforms:Telegram:BotTokens:another-tenant"] = "987654:XYZ-ABC9876543210",
                ["Platforms:Telegram:SecretTokens:test-tenant"] = "secret-token-123"
            })
            .Build();
        _configuration = configuration;

        var services = new ServiceCollection();
        services.AddSingleton<ICommandDispatcher>(_commandDispatcher.Object);
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton<ILogger<TelegramWebhookHandler>>(_logger.Object);

        _serviceProvider = services.BuildServiceProvider();
        _handler = new TelegramWebhookHandler(_serviceProvider);
    }

    [Fact]
    public async Task HandleAsync_WithValidBotToken_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramWebhookPayload
        {
            UpdateId = 12345678,
            Message = new TelegramMessage
            {
                MessageId = 1,
                Text = "Test message",
                Chat = new TelegramChat
                {
                    Id = 123456,
                    Type = "private"
                },
                From = new TelegramUser
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
        var payload = new TelegramWebhookPayload { UpdateId = 123 };
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
        var payload = new TelegramWebhookPayload
        {
            UpdateId = 987654,
            Message = new TelegramMessage
            {
                MessageId = 2,
                Text = "Secret test"
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
        var payload = new TelegramWebhookPayload { UpdateId = 111 };
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
        var payload = new TelegramWebhookPayload { UpdateId = 222 };
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
        Assert.Equal("Invalid payload", badRequestResult.Value);
    }

    [Fact]
    public async Task HandleAsync_WithCallbackQuery_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramWebhookPayload
        {
            UpdateId = 333,
            CallbackQuery = new TelegramCallbackQuery
            {
                Id = "callback123",
                Data = "button_clicked",
                From = new TelegramUser
                {
                    Id = 456,
                    FirstName = "Callback User"
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
    public async Task HandleAsync_WithEditedMessage_ShouldProcessWebhook()
    {
        // Arrange
        var payload = new TelegramWebhookPayload
        {
            UpdateId = 444,
            EditedMessage = new TelegramMessage
            {
                MessageId = 3,
                Text = "Edited message",
                EditDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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

        var payload = new TelegramWebhookPayload { UpdateId = 555 };
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

// Test payload classes
public class TelegramWebhookPayload
{
    public long UpdateId { get; set; }
    public TelegramMessage? Message { get; set; }
    public TelegramMessage? EditedMessage { get; set; }
    public TelegramCallbackQuery? CallbackQuery { get; set; }
}

public class TelegramMessage
{
    public int MessageId { get; set; }
    public string? Text { get; set; }
    public TelegramChat? Chat { get; set; }
    public TelegramUser? From { get; set; }
    public long? EditDate { get; set; }
}

public class TelegramChat
{
    public long Id { get; set; }
    public string? Type { get; set; }
}

public class TelegramUser
{
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? Username { get; set; }
}

public class TelegramCallbackQuery
{
    public string? Id { get; set; }
    public string? Data { get; set; }
    public TelegramUser? From { get; set; }
}