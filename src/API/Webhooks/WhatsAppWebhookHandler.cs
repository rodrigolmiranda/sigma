using System.Text.Json;
using Sigma.Shared.Contracts;

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
            // Handle verification request
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

            // Verify WhatsApp signature
            var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                return Results.BadRequest("Missing signature header");
            }

            if (!VerifyWhatsAppSignature(body, signature, appSecret))
            {
                return Results.Unauthorized();
            }

            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(body);
            if (payload == null)
            {
                return Results.BadRequest("Invalid payload");
            }

            // Log the webhook
            await LogWebhookAsync("WhatsApp", tenantId, payload);

            // Process messages
            if (payload.Entry != null)
            {
                foreach (var entry in payload.Entry)
                {
                    if (entry.Changes != null)
                    {
                        foreach (var change in entry.Changes)
                        {
                            if (change.Field == "messages")
                            {
                                // TODO: Process WhatsApp messages
                                // Note: Store only hashed phone numbers for privacy
                            }
                        }
                    }
                }
            }

            // TODO: Queue the message for processing
            // var queue = _services.GetRequiredService<IQueueService>();
            // await queue.EnqueueAsync(new MessageEvent { ... });

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
}