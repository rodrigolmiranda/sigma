using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sigma.Application.Services;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;

namespace Sigma.API.Webhooks;

public class TelegramWebhookHandler : WebhookHandlerBase
{
    public TelegramWebhookHandler(IServiceProvider services) : base(services)
    {
    }

    public override async Task<IResult> HandleAsync(string botToken, HttpContext context)
    {
        try
        {
            var body = await ReadRequestBodyAsync(context);

            // Validate bot token against configured tokens
            var configuration = _services.GetRequiredService<IConfiguration>();
            var configuredTokens = configuration.GetSection("Platforms:Telegram:BotTokens")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

            bool isValidToken = false;
            string? tenantId = null;
            string? workspaceId = null;

            foreach (var kvp in configuredTokens)
            {
                if (kvp.Value == botToken || kvp.Value.EndsWith(botToken))
                {
                    isValidToken = true;
                    // Format: "tenantId:workspaceId" or just "tenantId"
                    var parts = kvp.Key.Split(':', 2);
                    tenantId = parts[0];
                    workspaceId = parts.Length > 1 ? parts[1] : null;
                    break;
                }
            }

            if (!isValidToken || string.IsNullOrEmpty(tenantId))
            {
                return Results.Unauthorized();
            }

            // Additional validation using secret_token if configured
            var secretToken = context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(secretToken))
            {
                var expectedToken = configuration[$"Platforms:Telegram:SecretTokens:{tenantId}"];
                if (!string.IsNullOrEmpty(expectedToken) && secretToken != expectedToken)
                {
                    return Results.Unauthorized();
                }
            }

            // Deserialize to typed Telegram update
            var update = JsonSerializer.Deserialize<TelegramUpdate>(body);
            if (update == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            // Check for idempotency: Have we already processed this update_id?
            var webhookEventRepo = _services.GetRequiredService<IWebhookEventRepository>();
            var existingEvent = await webhookEventRepo.GetByExternalIdAsync(
                update.UpdateId.ToString(),
                "Telegram",
                Guid.Parse(tenantId)
            );

            if (existingEvent != null)
            {
                // Already processed, return success (idempotent)
                var logger = _services.GetService<ILogger<TelegramWebhookHandler>>();
                logger?.LogInformation("Duplicate Telegram update_id {UpdateId} for tenant {TenantId}, skipping",
                    update.UpdateId, tenantId);
                return Results.Ok();
            }

            // Get or create workspace for this bot
            var dbContext = _services.GetRequiredService<SigmaDbContext>();
            Guid workspaceGuid;

            if (!string.IsNullOrEmpty(workspaceId) && Guid.TryParse(workspaceId, out var parsedWorkspaceId))
            {
                workspaceGuid = parsedWorkspaceId;
            }
            else
            {
                // Find or create default Telegram workspace for tenant
                var tenantGuid = Guid.Parse(tenantId);
                var workspace = await dbContext.Workspaces
                    .Where(w => w.Platform == Platform.Telegram && EF.Property<Guid>(w, "TenantId") == tenantGuid)
                    .FirstOrDefaultAsync();

                if (workspace == null)
                {
                    // Create default workspace
                    var tenant = await dbContext.Tenants.FindAsync(tenantGuid);
                    if (tenant == null)
                    {
                        return Results.BadRequest($"Tenant {tenantId} not found");
                    }

                    workspace = tenant.AddWorkspace("Telegram", Platform.Telegram);
                    await dbContext.SaveChangesAsync();
                }

                workspaceGuid = workspace.Id;
            }

            // Normalize the Telegram message to canonical MessageEvent
            var normalizer = _services.GetRequiredService<IMessageNormalizer>();
            var messageEvent = normalizer.NormalizeTelegramMessage(update, Guid.Parse(tenantId), workspaceGuid);

            // Store webhook event for idempotency tracking
            var webhookEvent = new Sigma.Domain.Entities.WebhookEvent(
                "Telegram",
                Guid.Parse(tenantId),
                update.UpdateId.ToString(),
                body
            );
            await webhookEventRepo.AddAsync(webhookEvent);

            // Store the normalized message directly (for MVP, skip queue complexity)
            var messageRepo = _services.GetRequiredService<IMessageRepository>();

            // Find or create channel
            var telegramMessage = update.Message ?? update.EditedMessage ?? update.ChannelPost ?? update.EditedChannelPost;
            var channelExternalId = telegramMessage?.Chat?.Id.ToString();

            if (!string.IsNullOrEmpty(channelExternalId))
            {
                var workspace = await dbContext.Workspaces.FindAsync(workspaceGuid);
                if (workspace != null)
                {
                    var channel = workspace.Channels.FirstOrDefault(c => c.ExternalId == channelExternalId);
                    if (channel == null)
                    {
                        var channelName = telegramMessage?.Chat?.Title ?? telegramMessage?.Chat?.Username ?? "Direct Chat";
                        channel = workspace.AddChannel(channelName, channelExternalId);
                        await dbContext.SaveChangesAsync();

                        // Set TenantId shadow property
                        dbContext.Entry(channel).Property("TenantId").CurrentValue = Guid.Parse(tenantId);
                        await dbContext.SaveChangesAsync();
                    }

                    // Create Message entity from MessageEvent
                    var message = new Sigma.Domain.Entities.Message(
                        channel.Id,
                        Guid.Parse(tenantId),
                        messageEvent.PlatformMessageId,
                        new Sigma.Domain.ValueObjects.MessageSender(
                            messageEvent.Sender.PlatformUserId,
                            messageEvent.Sender.DisplayName ?? "Unknown",
                            messageEvent.Sender.IsBot
                        ),
                        MapToMessageType(messageEvent.Type),
                        messageEvent.Text ?? string.Empty,
                        messageEvent.TimestampUtc
                    );

                    await messageRepo.AddAsync(message);
                }
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<TelegramWebhookHandler>>();
            logger?.LogError(ex, "Error processing Telegram webhook");
            return Results.StatusCode(500);
        }
    }

    private static Sigma.Domain.ValueObjects.MessageType MapToMessageType(MessageEventType eventType)
    {
        return eventType switch
        {
            MessageEventType.Text => Sigma.Domain.ValueObjects.MessageType.Text,
            MessageEventType.Image => Sigma.Domain.ValueObjects.MessageType.Image,
            MessageEventType.File => Sigma.Domain.ValueObjects.MessageType.File,
            MessageEventType.System => Sigma.Domain.ValueObjects.MessageType.System,
            _ => Sigma.Domain.ValueObjects.MessageType.Text
        };
    }
}