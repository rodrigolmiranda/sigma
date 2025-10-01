using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sigma.Application.PlatformClients;

namespace Sigma.Infrastructure.PlatformClients;

public class WhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppClient> _logger;
    private const string BaseUrl = "https://graph.facebook.com/v18.0";

    public WhatsAppClient(HttpClient httpClient, ILogger<WhatsAppClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        if (string.IsNullOrEmpty(phoneNumberId))
            throw new ArgumentException("Phone number ID cannot be empty", nameof(phoneNumberId));

        if (string.IsNullOrEmpty(recipientPhoneNumber))
            throw new ArgumentException("Recipient phone number cannot be empty", nameof(recipientPhoneNumber));

        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Message text cannot be empty", nameof(text));

        try
        {
            var url = $"{BaseUrl}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = recipientPhoneNumber,
                type = "text",
                text = new { body = text }
            };

            var json = JsonSerializer.Serialize(payload);

            // Use HttpRequestMessage to avoid mutating shared HttpClient headers
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent message to WhatsApp number {PhoneNumber}", recipientPhoneNumber);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send WhatsApp message. Status: {Status}, Error: {Error}",
                response.StatusCode, error);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending WhatsApp message to {PhoneNumber}", recipientPhoneNumber);
            return false;
        }
    }
}