using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sigma.Domain.Contracts;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.API.Webhooks;

public abstract class WebhookHandlerBase
{
    protected readonly IServiceProvider _services;

    protected WebhookHandlerBase(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    protected async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
        return body;
    }

    protected bool VerifyHmacSignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToBase64String(computedHash);
        return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
    }

    protected bool VerifySlackSignature(string body, string timestamp, string signature, string signingSecret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("v0="))
            return false;

        var baseString = $"v0:{timestamp}:{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        var computedSignature = "v0=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

        return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
    }

    protected bool VerifyWhatsAppSignature(string body, string signature, string appSecret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var computedSignature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

        return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
    }

    protected Task LogWebhookAsync(string platform, string tenantId, object payload)
    {
        // TODO: Implement logging to Application Insights or other telemetry
        var logger = _services.GetService<ILogger<WebhookHandlerBase>>();
        logger?.LogInformation("Webhook received: Platform={Platform}, TenantId={TenantId}", platform, tenantId);
        return Task.CompletedTask;
    }

    protected async Task<(bool isDuplicate, WebhookEvent? existingEvent)> CheckIdempotencyAsync(
        string platform,
        string eventId,
        string eventType,
        string payload)
    {
        using var scope = _services.CreateScope();
        var webhookEventRepository = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existingEvent = await webhookEventRepository.GetByPlatformAndEventIdAsync(platform, eventId);

        if (existingEvent != null)
        {
            // Event already processed
            return (true, existingEvent);
        }

        // Create new event record
        var webhookEvent = new WebhookEvent(platform, eventId, eventType, payload);
        await webhookEventRepository.AddAsync(webhookEvent);

        try
        {
            await unitOfWork.SaveChangesAsync();
            return (false, webhookEvent);
        }
        catch (Exception ex) when (ex.Message.Contains("duplicate key") || ex.Message.Contains("unique constraint"))
        {
            // Race condition - another process already inserted this event
            existingEvent = await webhookEventRepository.GetByPlatformAndEventIdAsync(platform, eventId);
            return (true, existingEvent);
        }
    }

    protected async Task MarkEventAsProcessedAsync(string platform, string eventId)
    {
        using var scope = _services.CreateScope();
        var webhookEventRepository = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var webhookEvent = await webhookEventRepository.GetByPlatformAndEventIdAsync(platform, eventId);
        if (webhookEvent != null)
        {
            webhookEvent.MarkAsProcessed();
            await webhookEventRepository.UpdateAsync(webhookEvent);
            await unitOfWork.SaveChangesAsync();
        }
    }

    protected async Task MarkEventAsFailedAsync(string platform, string eventId, string error)
    {
        using var scope = _services.CreateScope();
        var webhookEventRepository = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var webhookEvent = await webhookEventRepository.GetByPlatformAndEventIdAsync(platform, eventId);
        if (webhookEvent != null)
        {
            webhookEvent.MarkAsFailed(error);
            await webhookEventRepository.UpdateAsync(webhookEvent);
            await unitOfWork.SaveChangesAsync();
        }
    }

    protected virtual string ExtractEventId(string platform, JsonDocument payload)
    {
        // Default implementation - platforms can override
        return platform.ToLower() switch
        {
            "slack" => payload.RootElement.TryGetProperty("event_id", out var slackId)
                ? slackId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString(),
            "discord" => payload.RootElement.TryGetProperty("id", out var discordId)
                ? discordId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString(),
            "telegram" => payload.RootElement.TryGetProperty("update_id", out var telegramId)
                ? telegramId.GetInt64().ToString()
                : Guid.NewGuid().ToString(),
            "whatsapp" => payload.RootElement.TryGetProperty("entry", out var entries) &&
                         entries.GetArrayLength() > 0 &&
                         entries[0].TryGetProperty("id", out var whatsappId)
                ? whatsappId.GetString() ?? Guid.NewGuid().ToString()
                : Guid.NewGuid().ToString(),
            _ => Guid.NewGuid().ToString()
        };
    }

    public abstract Task<IResult> HandleAsync(string identifier, HttpContext context);
}