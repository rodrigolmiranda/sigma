namespace Sigma.Application.PlatformClients;

/// <summary>
/// Telegram Bot API client for sending messages to Telegram chats and groups.
/// </summary>
public interface ITelegramClient
{
    /// <summary>
    /// Sends a text message to a Telegram chat or group.
    /// </summary>
    /// <param name="botToken">The Telegram bot token for authentication.</param>
    /// <param name="chatId">The chat ID to send the message to.</param>
    /// <param name="text">The message text to send.</param>
    /// <param name="parseMarkdown">Whether to parse Markdown formatting in the message text.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the message was sent successfully; otherwise, false.</returns>
    Task<bool> SendMessageAsync(
        string botToken,
        string chatId,
        string text,
        bool parseMarkdown = true,
        CancellationToken cancellationToken = default);
}
