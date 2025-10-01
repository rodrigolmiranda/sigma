using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sigma.Application.Services;
using Sigma.Domain.Repositories;
using Sigma.Infrastructure.Persistence;
using Sigma.Shared.Contracts;
using Sigma.Shared.Enums;

namespace Sigma.API.Webhooks;

public class WhatsAppWebhookHandler : WebhookHandlerBase
{
    public WhatsAppWebhookHandler(IServiceProvider services) : base(services)
    {
    }

    public override async Task<IResult> HandleAsync(string tenantId, HttpContext context)
    {
        try
        {
            // Handle verification request (GET)
            if (context.Request.Method == "GET")
            {
                return HandleVerification(context);
            }

            var body = await ReadRequestBodyAsync(context);

            // Get WhatsApp app secret from configuration
            var configuration = _services.GetRequiredService<IConfiguration>();
            var appSecret = configuration[$"Platforms:WhatsApp:AppSecrets:{tenantId}"]
                ?? configuration["Platforms:WhatsApp:DefaultAppSecret"];

            if (string.IsNullOrEmpty(appSecret))
            {
                return Results.Unauthorized();
            }

            // Verify WhatsApp HMAC signature
            var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                return Results.BadRequest("Missing signature header");
            }

            if (!VerifyWhatsAppSignature(body, signature, appSecret))
            {
                return Results.Unauthorized();
            }

            // Deserialize WhatsApp webhook payload
            var payload = JsonSerializer.Deserialize<WhatsAppWebhookEntry>(body);
            if (payload == null || payload.Changes == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            // Get tenant salt for phone number hashing
            var tenantSalt = configuration[$"Platforms:WhatsApp:TenantSalts:{tenantId}"]
                ?? configuration["Platforms:WhatsApp:DefaultSalt"]
                ?? tenantId; // Fallback to tenantId as salt

            var dbContext = _services.GetRequiredService<SigmaDbContext>();
            var webhookEventRepo = _services.GetRequiredService<IWebhookEventRepository>();
            var messageRepo = _services.GetRequiredService<IMessageRepository>();
            var normalizer = _services.GetRequiredService<IMessageNormalizer>();

            var tenantGuid = Guid.Parse(tenantId);

            // Get or create default WhatsApp workspace for tenant
            var workspace = await dbContext.Workspaces
                .Where(w => w.Platform == Platform.WhatsApp && EF.Property<Guid>(w, "TenantId") == tenantGuid)
                .FirstOrDefaultAsync();

            if (workspace == null)
            {
                var tenant = await dbContext.Tenants.FindAsync(tenantGuid);
                if (tenant == null)
                {
                    return Results.BadRequest($"Tenant {tenantId} not found");
                }

                workspace = tenant.AddWorkspace("WhatsApp", Platform.WhatsApp);
                await dbContext.SaveChangesAsync();
            }

            // Process each change (message event)
            foreach (var change in payload.Changes)
            {
                if (change.Value?.Messages != null && change.Value.Messages.Any())
                {
                    foreach (var whatsappMessage in change.Value.Messages)
                    {
                        // Check for idempotency
                        var existingEvent = await webhookEventRepo.GetByExternalIdAsync(
                            whatsappMessage.Id,
                            "WhatsApp",
                            tenantGuid
                        );

                        if (existingEvent != null)
                        {
                            // Already processed
                            continue;
                        }

                        // Store webhook event for idempotency
                        var webhookEvent = new Sigma.Domain.Entities.WebhookEvent(
                            "WhatsApp",
                            tenantGuid,
                            whatsappMessage.Id,
                            JsonSerializer.Serialize(whatsappMessage)
                        );
                        await webhookEventRepo.AddAsync(webhookEvent);

                        // Normalize WhatsApp message to canonical MessageEvent
                        var messageEvent = normalizer.NormalizeWhatsAppMessage(
                            whatsappMessage,
                            change.Value.Metadata!,
                            tenantGuid,
                            workspace.Id,
                            tenantSalt
                        );

                        // Find or create channel (pseudo-channel per user for WhatsApp)
                        var channelExternalId = messageEvent.PlatformChannelId; // wa:{hashed-phone}

                        if (!string.IsNullOrEmpty(channelExternalId))
                        {
                            var channel = workspace.Channels.FirstOrDefault(c => c.ExternalId == channelExternalId);
                            if (channel == null)
                            {
                                // Get contact name from WhatsApp webhook
                                var contactName = change.Value.Contacts?.FirstOrDefault()?.Profile?.Name
                                    ?? messageEvent.Sender.DisplayName ?? "WhatsApp User";

                                channel = workspace.AddChannel(contactName, channelExternalId);
                                await dbContext.SaveChangesAsync();

                                // Set TenantId shadow property
                                dbContext.Entry(channel).Property("TenantId").CurrentValue = tenantGuid;
                                await dbContext.SaveChangesAsync();
                            }

                            // Create Message entity from MessageEvent
                            var message = new Sigma.Domain.Entities.Message(
                                channel.Id,
                                tenantGuid,
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
                }
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<WhatsAppWebhookHandler>>();
            logger?.LogError(ex, "Error processing WhatsApp webhook");
            return Results.StatusCode(500);
        }
    }

    private IResult HandleVerification(HttpContext context)
    {
        var mode = context.Request.Query["hub.mode"];
        var token = context.Request.Query["hub.verify_token"];
        var challenge = context.Request.Query["hub.challenge"];

        var configuration = _services.GetRequiredService<IConfiguration>();
        var verifyToken = configuration["Platforms:WhatsApp:VerifyToken"];

        if (mode == "subscribe" && token == verifyToken)
        {
            return Results.Ok(challenge.ToString());
        }

        return Results.StatusCode(403);
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