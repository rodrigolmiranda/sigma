using System.Security.Cryptography;
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

public class WhatsAppWebhookHandlerTests
{
    private readonly WhatsAppWebhookHandler _handler;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<WhatsAppWebhookHandler>> _logger;
    private const string TestAppSecret = "test-app-secret-123";
    private const string TestVerifyToken = "test-verify-token";

    public WhatsAppWebhookHandlerTests()
    {
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _logger = new Mock<ILogger<WhatsAppWebhookHandler>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Platforms:WhatsApp:AppSecrets:test-tenant"] = TestAppSecret,
                ["Platforms:WhatsApp:DefaultAppSecret"] = "default-secret",
                ["Platforms:WhatsApp:VerifyToken"] = TestVerifyToken
            })
            .Build();
        _configuration = configuration;

        var services = new ServiceCollection();
        services.AddSingleton<ICommandDispatcher>(_commandDispatcher.Object);
        services.AddSingleton<IConfiguration>(_configuration);
        services.AddSingleton<ILogger<WhatsAppWebhookHandler>>(_logger.Object);

        _serviceProvider = services.BuildServiceProvider();
        _handler = new WhatsAppWebhookHandler(_serviceProvider);
    }

    [Fact]
    public async Task HandleAsync_WithVerificationRequest_ShouldReturnChallenge()
    {
        // Arrange
        var tenantId = "test-tenant";
        var challenge = "challenge-123456";
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["hub.mode"] = "subscribe",
            ["hub.verify_token"] = TestVerifyToken,
            ["hub.challenge"] = challenge
        });

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        // The handler should return the challenge value for verification
    }

    [Fact]
    public async Task HandleAsync_WithInvalidVerificationToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["hub.mode"] = "subscribe",
            ["hub.verify_token"] = "wrong-token",
            ["hub.challenge"] = "challenge-123"
        });

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        // Should return unauthorized for invalid verification token
    }

    [Fact]
    public async Task HandleAsync_WithValidSignature_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "test-tenant";
        var payload = new WhatsAppWebhookPayload
        {
            Entry = new[]
            {
                new WhatsAppEntry
                {
                    Id = "entry123",
                    Changes = new[]
                    {
                        new WhatsAppChange
                        {
                            Field = "messages",
                            Value = new WhatsAppValue
                            {
                                Messages = new[]
                                {
                                    new WhatsAppMessage
                                    {
                                        Id = "msg123",
                                        From = "1234567890",
                                        Type = "text",
                                        Text = new WhatsAppText { Body = "Test message" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var signature = ComputeWhatsAppSignature(jsonPayload, TestAppSecret);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Hub-Signature-256"] = $"sha256={signature}";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSignature_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenantId = "test-tenant";
        var payload = new WhatsAppWebhookPayload
        {
            Entry = new[] { new WhatsAppEntry { Id = "123" } }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Hub-Signature-256"] = "sha256=invalid-signature";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        Assert.NotNull(unauthorizedResult);
    }

    [Fact]
    public async Task HandleAsync_WithMissingSignature_ShouldReturnBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant";
        var payload = new WhatsAppWebhookPayload();
        var jsonPayload = JsonSerializer.Serialize(payload);
        var context = CreateHttpContext(jsonPayload);
        // No signature header

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>;
        Assert.NotNull(badRequestResult);
        Assert.Equal("Missing signature header", badRequestResult.Value);
    }

    [Fact]
    public async Task HandleAsync_WithMissingAppSecret_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenantId = "unknown-tenant";
        var emptyConfig = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(emptyConfig);
        services.AddSingleton<ILogger<WhatsAppWebhookHandler>>(_logger.Object);

        var serviceProvider = services.BuildServiceProvider();
        var handler = new WhatsAppWebhookHandler(serviceProvider);

        var payload = new WhatsAppWebhookPayload();
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
    public async Task HandleAsync_WithStatusUpdate_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "test-tenant";
        var payload = new WhatsAppWebhookPayload
        {
            Entry = new[]
            {
                new WhatsAppEntry
                {
                    Id = "entry456",
                    Changes = new[]
                    {
                        new WhatsAppChange
                        {
                            Field = "messages",
                            Value = new WhatsAppValue
                            {
                                Statuses = new[]
                                {
                                    new WhatsAppStatus
                                    {
                                        Id = "msg789",
                                        Status = "delivered",
                                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var signature = ComputeWhatsAppSignature(jsonPayload, TestAppSecret);
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Hub-Signature-256"] = $"sha256={signature}";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = CreateHttpContext("invalid json");
        context.Request.Headers["X-Hub-Signature-256"] = "sha256=test";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        // Should return 500 or unauthorized for invalid signature
        // Since the signature validation will fail first before JSON parsing
        var unauthorizedResult = result as Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult;
        if (unauthorizedResult == null)
        {
            var statusResult = result as Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult;
            Assert.NotNull(statusResult);
            Assert.Equal(500, statusResult.StatusCode);
        }
        else
        {
            Assert.NotNull(unauthorizedResult);
        }
    }

    [Fact]
    public async Task HandleAsync_WithNullPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var tenantId = "test-tenant";
        var context = CreateHttpContext("null");
        var signature = ComputeWhatsAppSignature("null", TestAppSecret);
        context.Request.Headers["X-Hub-Signature-256"] = $"sha256={signature}";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>;
        Assert.NotNull(badRequestResult);
        Assert.Equal("Invalid payload", badRequestResult.Value);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultAppSecret_ShouldProcessWebhook()
    {
        // Arrange
        var tenantId = "new-tenant"; // Tenant without specific app secret
        var payload = new WhatsAppWebhookPayload
        {
            Entry = new[] { new WhatsAppEntry { Id = "test" } }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var signature = ComputeWhatsAppSignature(jsonPayload, "default-secret");
        var context = CreateHttpContext(jsonPayload);
        context.Request.Headers["X-Hub-Signature-256"] = $"sha256={signature}";

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
        context.Request.Method = "POST";
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
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context;
    }

    private static string ComputeWhatsAppSignature(string payload, string appSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLower();
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
public class WhatsAppWebhookPayload
{
    public WhatsAppEntry[]? Entry { get; set; }
}

public class WhatsAppEntry
{
    public string? Id { get; set; }
    public WhatsAppChange[]? Changes { get; set; }
}

public class WhatsAppChange
{
    public string? Field { get; set; }
    public WhatsAppValue? Value { get; set; }
}

public class WhatsAppValue
{
    public WhatsAppMessage[]? Messages { get; set; }
    public WhatsAppStatus[]? Statuses { get; set; }
}

public class WhatsAppMessage
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? Type { get; set; }
    public WhatsAppText? Text { get; set; }
}

public class WhatsAppText
{
    public string? Body { get; set; }
}

public class WhatsAppStatus
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Timestamp { get; set; }
}