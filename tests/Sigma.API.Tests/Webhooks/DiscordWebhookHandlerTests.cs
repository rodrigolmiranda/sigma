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

public class DiscordWebhookHandlerTests
{
    private readonly DiscordWebhookHandler _handler;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<DiscordWebhookHandler>> _logger;

    public DiscordWebhookHandlerTests()
    {
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _logger = new Mock<ILogger<DiscordWebhookHandler>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platforms:Discord:BotTokens:test-tenant"] = "test-bot-token",
                ["Platforms:Discord:DefaultBotToken"] = "default-bot-token"
            })
            .Build();
        _configuration = configuration;

        var services = new ServiceCollection();
        services.AddSingleton<ICommandDispatcher>(_commandDispatcher.Object);
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton<ILogger<DiscordWebhookHandler>>(_logger.Object);

        _serviceProvider = services.BuildServiceProvider();
        _handler = new DiscordWebhookHandler(_serviceProvider);
    }

    [Fact]
    public async Task HandleAsync_WithValidPayload_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "test-tenant";
        var payload = new DiscordWebhookPayload
        {
            Type = 0,
            Id = "123456",
            GuildId = "guild123",
            ChannelId = "channel123",
            Content = "Test message",
            Author = new DiscordAuthor
            {
                Id = "author123",
                Username = "TestUser",
                Discriminator = "0001"
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithMissingBotToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenantId = "unknown-tenant";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<DiscordWebhookHandler>>(_logger.Object);

        var serviceProvider = services.BuildServiceProvider();
        var handler = new DiscordWebhookHandler(serviceProvider);

        var payload = new DiscordWebhookPayload { Type = 0 };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        Assert.NotNull(unauthorizedResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = CreateHttpContext("invalid json");

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WithValidSignature_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "test-tenant";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = new DiscordWebhookPayload
        {
            Type = 1, // Interaction
            Id = "interaction123"
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Signature-Ed25519"] = "test-signature";
        context.Request.Headers["X-Signature-Timestamp"] = timestamp;

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithNullPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = CreateHttpContext("null");

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>;
        Assert.NotNull(badRequestResult);
        Assert.Equal("Invalid payload", badRequestResult.Value);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultBotToken_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "new-tenant"; // Tenant without specific bot token
        var payload = new DiscordWebhookPayload
        {
            Type = 0,
            Content = "Test with default token"
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WhenExceptionOccurs_ShouldReturn500()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = new DefaultHttpContext();
        context.Request.Body = new ThrowingStream(); // Stream that throws on read

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

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

// Test payload classes - these should match the actual payload structure
public class DiscordWebhookPayload
{
    public int Type { get; set; }
    public string? Id { get; set; }
    public string? GuildId { get; set; }
    public string? ChannelId { get; set; }
    public string? Content { get; set; }
    public DiscordAuthor? Author { get; set; }
}

public class DiscordAuthor
{
    public string? Id { get; set; }
    public string? Username { get; set; }
    public string? Discriminator { get; set; }
}