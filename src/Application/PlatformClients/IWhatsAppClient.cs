namespace Sigma.Application.PlatformClients;

/// <summary>
/// WhatsApp Business API client for sending messages via WhatsApp Cloud API.
/// </summary>
public interface IWhatsAppClient
{
    /// <summary>
    /// Sends a text message to a WhatsApp recipient.
    /// </summary>
    /// <param name="accessToken">The WhatsApp Business API access token.</param>
    /// <param name="phoneNumberId">The phone number ID from WhatsApp Business API.</param>
    /// <param name="recipientPhoneNumber">The recipient's phone number in E.164 format.</param>
    /// <param name="text">The message text to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if the message was sent successfully; otherwise, false.</returns>
    Task<bool> SendMessageAsync(
        string accessToken,
        string phoneNumberId,
        string recipientPhoneNumber,
        string text,
        CancellationToken cancellationToken = default);
}
