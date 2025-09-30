using System.Text.Json;
using Sigma.Shared.Contracts;

namespace Sigma.API.Webhooks;

public class DiscordWebhookHandler : WebhookHandlerBase
{
    public DiscordWebhookHandler(IServiceProvider services) : base(services)
    {
    }

    public override async Task<IResult> HandleAsync(string tenantId, HttpContext context)
    {
        try
        {
            var body = await ReadRequestBodyAsync(context);

            // Get Discord bot token from configuration
            var configuration = _services.GetRequiredService<IConfiguration>();
            var botToken = configuration[$"Platforms:Discord:BotTokens:{tenantId}"]
                ?? configuration["Platforms:Discord:DefaultBotToken"];

            if (string.IsNullOrEmpty(botToken))
            {
                return Results.Unauthorized();
            }

            // Verify Discord signature (if using interactions endpoint)
            var signature = context.Request.Headers["X-Signature-Ed25519"].FirstOrDefault();
            var timestamp = context.Request.Headers["X-Signature-Timestamp"].FirstOrDefault();

            // For Discord gateway events, we typically don't have signature validation
            // as they come through WebSocket. This endpoint would be for interactions.

            var payload = JsonSerializer.Deserialize<DiscordWebhookPayload>(body);
            if (payload == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            // Log the webhook
            await LogWebhookAsync("Discord", tenantId, payload);

            // TODO: Queue the message for processing
            // var queue = _services.GetRequiredService<IQueueService>();
            // await queue.EnqueueAsync(new MessageEvent { ... });

            return Results.Ok();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<DiscordWebhookHandler>>();
            logger?.LogError(ex, "Error processing Discord webhook");
            return Results.StatusCode(500);
        }
    }
}