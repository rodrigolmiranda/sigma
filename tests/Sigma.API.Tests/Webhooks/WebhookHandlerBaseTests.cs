using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Sigma.API.Webhooks;
using System.Text;
using Xunit;

namespace Sigma.API.Tests.Webhooks;

public class WebhookHandlerBaseTests
{
    private readonly TestWebhookHandler _handler;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<ILogger<WebhookHandlerBase>> _logger;

    public WebhookHandlerBaseTests()
    {
        _serviceProvider = new Mock<IServiceProvider>();
        _logger = new Mock<ILogger<WebhookHandlerBase>>();
        _serviceProvider.Setup(sp => sp.GetService(typeof(ILogger<WebhookHandlerBase>)))
            .Returns(_logger.Object);
        _handler = new TestWebhookHandler(_serviceProvider.Object);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithSimpleBody_ShouldReturnCorrectContent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var content = "Test webhook body content";
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Request.Body = new MemoryStream(bytes);

        // Act
        var result = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(content, result);
        Assert.Equal(0, context.Request.Body.Position); // Should reset position
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithEmptyBody_ShouldReturnEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream();

        // Act
        var result = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithLargeBody_ShouldHandleCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var content = new string('x', 100000); // 100KB
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Request.Body = new MemoryStream(bytes);

        // Act
        var result = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void VerifyHmacSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var payload = "test payload";
        var secret = "secret123";
        var signature = _handler.TestGenerateHmacSignature(payload, secret);

        // Act
        var result = _handler.TestVerifyHmacSignature(payload, signature, secret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyHmacSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var payload = "test payload";
        var secret = "secret123";
        var signature = "invalid_signature";

        // Act
        var result = _handler.TestVerifyHmacSignature(payload, signature, secret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyHmacSignature_WithWrongSecret_ShouldReturnFalse()
    {
        // Arrange
        var payload = "test payload";
        var correctSecret = "secret123";
        var wrongSecret = "wrong_secret";
        var signature = _handler.TestGenerateHmacSignature(payload, correctSecret);

        // Act
        var result = _handler.TestVerifyHmacSignature(payload, signature, wrongSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySlackSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";
        var signature = _handler.TestGenerateSlackSignature(body, timestamp, signingSecret);

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, signature, signingSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySlackSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, "v0=invalid", signingSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySlackSignature_WithoutV0Prefix_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, "invalid_format", signingSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySlackSignature_WithEmptySignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, string.Empty, signingSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";
        var signature = _handler.TestGenerateWhatsAppSignature(body, appSecret);

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, "sha256=invalid", appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithoutSha256Prefix_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, "invalid_format", appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LogWebhookAsync_WithValidData_ShouldLogInformation()
    {
        // Arrange
        var platform = "Slack";
        var tenantId = "tenant-123";
        var payload = new { test = "data" };

        // Act
        await _handler.TestLogWebhookAsync(platform, tenantId, payload);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogWebhookAsync_WithNullLogger_ShouldNotThrow()
    {
        // Arrange
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(ILogger<WebhookHandlerBase>)))
            .Returns((ILogger<WebhookHandlerBase>?)null);
        var handler = new TestWebhookHandler(serviceProvider.Object);

        var platform = "Slack";
        var tenantId = "tenant-123";
        var payload = new { test = "data" };

        // Act & Assert (should not throw)
        await handler.TestLogWebhookAsync(platform, tenantId, payload);
    }

    [Theory]
    [InlineData(null, "tenant", "payload")]
    [InlineData("platform", null, "payload")]
    [InlineData("platform", "tenant", null)]
    public async Task LogWebhookAsync_WithNullParameters_ShouldNotThrow(string? platform, string? tenantId, object? payload)
    {
        // Act & Assert (should not throw)
        await _handler.TestLogWebhookAsync(platform!, tenantId!, payload!);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_MultipleTimes_ShouldReturnSameContent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var content = "Test content that should be read multiple times";
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Request.Body = new MemoryStream(bytes);

        // Act
        var result1 = await _handler.TestReadRequestBodyAsync(context);
        var result2 = await _handler.TestReadRequestBodyAsync(context);
        var result3 = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(content, result1);
        Assert.Equal(content, result2);
        Assert.Equal(content, result3);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithUnicodeContent_ShouldHandleCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var content = "Unicode test: 你好世界 مرحبا بالعالم שלום עולם 🌍🎉💻";
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Request.Body = new MemoryStream(bytes);

        // Act
        var result = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void VerifyHmacSignature_WithEmptyPayload_ShouldWork()
    {
        // Arrange
        var payload = "";
        var secret = "secret123";
        var signature = _handler.TestGenerateHmacSignature(payload, secret);

        // Act
        var result = _handler.TestVerifyHmacSignature(payload, signature, secret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyHmacSignature_CaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var payload = "test payload";
        var secret = "secret123";
        var signature = _handler.TestGenerateHmacSignature(payload, secret);

        // Act
        var result1 = _handler.TestVerifyHmacSignature(payload, signature.ToLower(), secret);
        var result2 = _handler.TestVerifyHmacSignature(payload, signature.ToUpper(), secret);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public void VerifySlackSignature_WithNullSignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, null!, signingSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySlackSignature_WithDifferentCaseHexOnly_ShouldReturnTrue()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";
        var signature = _handler.TestGenerateSlackSignature(body, timestamp, signingSecret);

        // The hex part after v0= can be in different case
        var parts = signature.Split('=');
        var signatureWithUpperHex = parts[0] + "=" + parts[1].ToUpper();

        // Act
        var result1 = _handler.TestVerifySlackSignature(body, timestamp, signature, signingSecret);
        var result2 = _handler.TestVerifySlackSignature(body, timestamp, signatureWithUpperHex, signingSecret);

        // Assert
        Assert.True(result1); // lowercase hex should work
        Assert.True(result2); // uppercase hex should also work due to OrdinalIgnoreCase
    }

    [Fact]
    public void VerifySlackSignature_WithUppercasePrefix_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "test_signing_secret";
        var signature = _handler.TestGenerateSlackSignature(body, timestamp, signingSecret);
        var signatureWithUpperPrefix = signature.Replace("v0=", "V0=");

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, signatureWithUpperPrefix, signingSecret);

        // Assert
        Assert.False(result); // Should fail because prefix must be lowercase "v0="
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithNullSignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, null!, appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithEmptySignature_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, "", appSecret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithDifferentCaseHexOnly_ShouldReturnTrue()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";
        var signature = _handler.TestGenerateWhatsAppSignature(body, appSecret);

        // The hex part after sha256= can be in different case
        var parts = signature.Split('=');
        var signatureWithUpperHex = parts[0] + "=" + parts[1].ToUpper();

        // Act
        var result1 = _handler.TestVerifyWhatsAppSignature(body, signature, appSecret);
        var result2 = _handler.TestVerifyWhatsAppSignature(body, signatureWithUpperHex, appSecret);

        // Assert
        Assert.True(result1); // lowercase hex should work
        Assert.True(result2); // uppercase hex should also work due to OrdinalIgnoreCase
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithUppercasePrefix_ShouldReturnFalse()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "whatsapp_app_secret";
        var signature = _handler.TestGenerateWhatsAppSignature(body, appSecret);
        var signatureWithUpperPrefix = signature.Replace("sha256=", "SHA256=");

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, signatureWithUpperPrefix, appSecret);

        // Assert
        Assert.False(result); // Should fail because prefix must be lowercase "sha256="
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestWebhookHandler(null!));
    }

    [Fact]
    public async Task LogWebhookAsync_WithComplexPayload_ShouldLogCorrectly()
    {
        // Arrange
        var platform = "Discord";
        var tenantId = "tenant-456";
        var payload = new
        {
            id = Guid.NewGuid(),
            nested = new
            {
                array = new[] { 1, 2, 3 },
                dict = new Dictionary<string, object>
                {
                    ["key1"] = "value1",
                    ["key2"] = 123,
                    ["key3"] = true
                }
            },
            timestamp = DateTime.UtcNow
        };

        // Act
        await _handler.TestLogWebhookAsync(platform, tenantId, payload);

        // Assert
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Webhook received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task ReadRequestBodyAsync_WithWhitespaceContent_ShouldReturnAsIs(string content)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(content);
        context.Request.Body = new MemoryStream(bytes);

        // Act
        var result = await _handler.TestReadRequestBodyAsync(context);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void VerifyHmacSignature_WithSpecialCharactersInSecret_ShouldWork()
    {
        // Arrange
        var payload = "test payload";
        var secret = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var signature = _handler.TestGenerateHmacSignature(payload, secret);

        // Act
        var result = _handler.TestVerifyHmacSignature(payload, signature, secret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySlackSignature_WithSpecialCharactersInSecret_ShouldWork()
    {
        // Arrange
        var body = "{\"test\":\"data\"}";
        var timestamp = "1234567890";
        var signingSecret = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var signature = _handler.TestGenerateSlackSignature(body, timestamp, signingSecret);

        // Act
        var result = _handler.TestVerifySlackSignature(body, timestamp, signature, signingSecret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyWhatsAppSignature_WithSpecialCharactersInSecret_ShouldWork()
    {
        // Arrange
        var body = "{\"entry\":[{\"messaging\":[]}]}";
        var appSecret = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var signature = _handler.TestGenerateWhatsAppSignature(body, appSecret);

        // Act
        var result = _handler.TestVerifyWhatsAppSignature(body, signature, appSecret);

        // Assert
        Assert.True(result);
    }

    // Test implementation class
    private class TestWebhookHandler : API.Webhooks.WebhookHandlerBase
    {
        public TestWebhookHandler(IServiceProvider services) : base(services)
        {
        }

        public override Task<IResult> HandleAsync(string identifier, HttpContext context)
        {
            return Task.FromResult(Results.Ok() as IResult);
        }

        public async Task<string> TestReadRequestBodyAsync(HttpContext context)
        {
            return await ReadRequestBodyAsync(context);
        }

        public bool TestVerifyHmacSignature(string payload, string signature, string secret)
        {
            return VerifyHmacSignature(payload, signature, secret);
        }

        public bool TestVerifySlackSignature(string body, string timestamp, string signature, string signingSecret)
        {
            return VerifySlackSignature(body, timestamp, signature, signingSecret);
        }

        public bool TestVerifyWhatsAppSignature(string body, string signature, string appSecret)
        {
            return VerifyWhatsAppSignature(body, signature, appSecret);
        }

        public async Task TestLogWebhookAsync(string platform, string tenantId, object payload)
        {
            await LogWebhookAsync(platform, tenantId, payload);
        }

        // Helper methods for generating signatures for testing
        public string TestGenerateHmacSignature(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(computedHash);
        }

        public string TestGenerateSlackSignature(string body, string timestamp, string signingSecret)
        {
            var baseString = $"v0:{timestamp}:{body}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            return "v0=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public string TestGenerateWhatsAppSignature(string body, string appSecret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}