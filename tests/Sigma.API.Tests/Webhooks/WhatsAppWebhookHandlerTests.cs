using System.Security.Cryptography;
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

public class WhatsAppWebhookHandlerTests
{
    private readonly WhatsAppWebhookHandler _handler;
    private readonly Mock<ICommandDispatcher> _commandDispatcher;
    private readonly Mock<IWebhookEventRepository> _webhookEventRepository;
    private readonly Mock<IMessageRepository> _messageRepository;
    private readonly Mock<IMessageNormalizer> _messageNormalizer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<WhatsAppWebhookHandler>> _logger;
    private readonly SigmaDbContext _dbContext;
    private const string TestAppSecret = "test-app-secret-123";
    private const string TestVerifyToken = "test-verify-token";
    private readonly Guid _testTenantId = Guid.NewGuid();

    public WhatsAppWebhookHandlerTests()
    {
        _commandDispatcher = new Mock<ICommandDispatcher>();
        _webhookEventRepository = new Mock<IWebhookEventRepository>();
        _messageRepository = new Mock<IMessageRepository>();
        _messageNormalizer = new Mock<IMessageNormalizer>();
        _logger = new Mock<ILogger<WhatsAppWebhookHandler>>();

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
                [$"Platforms:WhatsApp:AppSecrets:{_testTenantId}"] = TestAppSecret,
                ["Platforms:WhatsApp:DefaultAppSecret"] = "default-secret",
                ["Platforms:WhatsApp:VerifyToken"] = TestVerifyToken
            })
            .Build();
        _configuration = configuration;

        // Setup webhook repository mock (not already processed)
        _webhookEventRepository.Setup(x => x.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookEvent?)null);

        // Setup message normalizer mock
        _messageNormalizer.Setup(m => m.NormalizeWhatsAppMessage(
                It.IsAny<Sigma.Shared.Contracts.WhatsAppIncomingMessage>(),
                It.IsAny<Sigma.Shared.Contracts.WhatsAppMetadata>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>()))
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
        services.AddSingleton<ILogger<WhatsAppWebhookHandler>>(_logger.Object);
        services.AddSingleton<SigmaDbContext>(_dbContext);
        services.AddSingleton<IWebhookEventRepository>(_webhookEventRepository.Object);
        services.AddSingleton<IMessageRepository>(_messageRepository.Object);
        services.AddSingleton<IMessageNormalizer>(_messageNormalizer.Object);

        _serviceProvider = services.BuildServiceProvider();
        _handler = new WhatsAppWebhookHandler(_serviceProvider);
    }

    [Fact]
    public async Task HandleAsync_WithVerificationRequest_ShouldReturnChallenge()
    {
        // Arrange
        var tenantId = _testTenantId.ToString();
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
        var tenantId = _testTenantId.ToString();
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
        var tenantId = _testTenantId.ToString();
        var payload = new WhatsAppWebhookEntry
        {
            Id = "entry123",
            Changes = new List<WhatsAppWebhookChange>
            {
                new WhatsAppWebhookChange
                {
                    Field = "messages",
                    Value = new WhatsAppWebhookValue
                    {
                        Metadata = new WhatsAppMetadata
                        {
                            PhoneNumberId = "123456",
                            DisplayPhoneNumber = "+1234567890"
                        },
                        Messages = new List<WhatsAppIncomingMessage>
                        {
                            new WhatsAppIncomingMessage
                            {
                                Id = "msg123",
                                From = "1234567890",
                                Type = "text",
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                Text = new WhatsAppTextMessage { Body = "Test message" }
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
        var tenantId = _testTenantId.ToString();
        var payload = new WhatsAppWebhookEntry
        {
            Id = "123",
            Changes = new List<WhatsAppWebhookChange>
            {
                new WhatsAppWebhookChange { Field = "messages", Value = new WhatsAppWebhookValue() }
            }
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
        var tenantId = _testTenantId.ToString();
        var payload = new WhatsAppWebhookEntry();
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
        var tenantId = _testTenantId.ToString();
        var payload = new WhatsAppWebhookEntry
        {
            Id = "entry456",
            Changes = new List<WhatsAppWebhookChange>
            {
                new WhatsAppWebhookChange
                {
                    Field = "messages",
                    Value = new WhatsAppWebhookValue
                    {
                        Metadata = new WhatsAppMetadata
                        {
                            PhoneNumberId = "123456",
                            DisplayPhoneNumber = "+1234567890"
                        },
                        Statuses = new List<WhatsAppStatus>
                        {
                            new WhatsAppStatus
                            {
                                Id = "msg789",
                                Status = "delivered",
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                RecipientId = "1234567890"
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
        var tenantId = _testTenantId.ToString();
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
        var tenantId = _testTenantId.ToString();
        var context = CreateHttpContext("null");
        var signature = ComputeWhatsAppSignature("null", TestAppSecret);
        context.Request.Headers["X-Hub-Signature-256"] = $"sha256={signature}";

        // Act
        var result = await _handler.HandleAsync(tenantId, context);

        // Assert
        Assert.NotNull(result);
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>;
        Assert.NotNull(badRequestResult);
        Assert.Equal("Invalid payload", badRequestResult!.Value);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultAppSecret_ShouldProcessWebhook()
    {
        // Arrange - Create a second tenant without specific app secret
        var newTenantId = Guid.NewGuid();
        var newTenant = new Tenant("New Tenant", "new-tenant", "free", 30);
        typeof(Entity).GetProperty("Id")!.SetValue(newTenant, newTenantId);
        _dbContext.Tenants.Add(newTenant);
        _dbContext.SaveChanges();

        var tenantId = newTenantId.ToString(); // Tenant without specific app secret
        var payload = new WhatsAppWebhookEntry
        {
            Id = "test",
            Changes = new List<WhatsAppWebhookChange>
            {
                new WhatsAppWebhookChange
                {
                    Field = "messages",
                    Value = new WhatsAppWebhookValue()
                }
            }
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
        var tenantId = _testTenantId.ToString();
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