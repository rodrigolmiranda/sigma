using System.Text.Json;
using Sigma.Shared.Contracts;

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

            foreach (var kvp in configuredTokens)
            {
                if (kvp.Value == botToken || kvp.Value.EndsWith(botToken))
                {
                    isValidToken = true;
                    tenantId = kvp.Key;
                    break;
                }
            }

            if (!isValidToken || string.IsNullOrEmpty(tenantId))
            {
                return Results.Unauthorized();
            }

            // Telegram uses the bot token in the URL path for security
            // Additional validation can be done using the secret_token if configured

            var secretToken = context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(secretToken))
            {
                var expectedToken = configuration[$"Platforms:Telegram:SecretTokens:{tenantId}"];
                if (!string.IsNullOrEmpty(expectedToken) && secretToken != expectedToken)
                {
                    return Results.Unauthorized();
                }
            }

            var payload = JsonSerializer.Deserialize<TelegramWebhookPayload>(body);
            if (payload == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            // Log the webhook
            await LogWebhookAsync("Telegram", tenantId, payload);

            // Store update_id for idempotency
            // TODO: Track update_id to prevent duplicate processing

            // TODO: Queue the message for processing
            // var queue = _services.GetRequiredService<IQueueService>();
            // await queue.EnqueueAsync(new MessageEvent { ... });

            return Results.Ok();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<TelegramWebhookHandler>>();
            logger?.LogError(ex, "Error processing Telegram webhook");
            return Results.StatusCode(500);
        }
    }
}