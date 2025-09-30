using System.Text.Json;
using Sigma.Shared.Contracts;

namespace Sigma.API.Webhooks;

public class SlackWebhookHandler : WebhookHandlerBase
{
    public SlackWebhookHandler(IServiceProvider services) : base(services)
    {
    }

    public override async Task<IResult> HandleAsync(string tenantId, HttpContext context)
    {
        try
        {
            var body = await ReadRequestBodyAsync(context);

            // Check for empty body
            if (string.IsNullOrWhiteSpace(body))
            {
                return Results.BadRequest("Empty request body");
            }

            // Get Slack signing secret from configuration
            var configuration = _services.GetRequiredService<IConfiguration>();
            var signingSecret = configuration[$"Platforms:Slack:SigningSecrets:{tenantId}"]
                ?? configuration["Platforms:Slack:DefaultSigningSecret"];

            if (string.IsNullOrEmpty(signingSecret))
            {
                return Results.Unauthorized();
            }

            // Verify Slack signature
            var timestamp = context.Request.Headers["X-Slack-Request-Timestamp"].FirstOrDefault();
            var signature = context.Request.Headers["X-Slack-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
            {
                return Results.Unauthorized();
            }

            // Check timestamp to prevent replay attacks (must be within 5 minutes)
            if (long.TryParse(timestamp, out var timestampSeconds))
            {
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
                var timeDiff = Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes);
                if (timeDiff > 5)
                {
                    return Results.BadRequest("Request timestamp too old");
                }
            }
            // If timestamp parsing fails, continue to signature validation which will likely fail

            if (!VerifySlackSignature(body, timestamp, signature, signingSecret))
            {
                return Results.Unauthorized();
            }

            SlackWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<SlackWebhookPayload>(body);
                if (payload == null)
                {
                    return Results.BadRequest("Invalid payload");
                }
            }
            catch (JsonException)
            {
                return Results.BadRequest("Malformed JSON");
            }

            // Handle URL verification challenge
            if (payload.Type == "url_verification" && !string.IsNullOrEmpty(payload.Challenge))
            {
                return Results.Ok(new { challenge = payload.Challenge });
            }

            // Log the webhook
            await LogWebhookAsync("Slack", tenantId, payload);

            // TODO: Queue the message for processing
            // var queue = _services.GetRequiredService<IQueueService>();
            // await queue.EnqueueAsync(new MessageEvent { ... });

            return Results.Ok();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<ILogger<SlackWebhookHandler>>();
            logger?.LogError(ex, "Error processing Slack webhook");
            return Results.StatusCode(500);
        }
    }
}