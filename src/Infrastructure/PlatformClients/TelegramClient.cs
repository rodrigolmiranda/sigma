using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sigma.Application.PlatformClients;

namespace Sigma.Infrastructure.PlatformClients;

public class TelegramClient : ITelegramClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramClient> _logger;

    public TelegramClient(HttpClient httpClient, ILogger<TelegramClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendMessageAsync(
        string botToken,
        string chatId,
        string text,
        bool parseMarkdown = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(botToken))
            throw new ArgumentException("Bot token cannot be empty", nameof(botToken));

        if (string.IsNullOrEmpty(chatId))
            throw new ArgumentException("Chat ID cannot be empty", nameof(chatId));

        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Message text cannot be empty", nameof(text));

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = text,
                parse_mode = parseMarkdown ? "Markdown" : null,
                disable_web_page_preview = true
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent message to Telegram chat {ChatId}", chatId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send Telegram message. Status: {Status}, Error: {Error}",
                response.StatusCode, error);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending Telegram message to chat {ChatId}", chatId);
            return false;
        }
    }
}