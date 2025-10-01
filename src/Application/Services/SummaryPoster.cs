using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sigma.Application.PlatformClients;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.Application.Services;

public class SummaryPoster : ISummaryPoster
{
    private readonly ISummaryGenerator _summaryGenerator;
    private readonly IChannelRepository _channelRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITelegramClient _telegramClient;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SummaryPoster> _logger;

    public SummaryPoster(
        ISummaryGenerator summaryGenerator,
        IChannelRepository channelRepository,
        IWorkspaceRepository workspaceRepository,
        ITelegramClient telegramClient,
        IWhatsAppClient whatsAppClient,
        IConfiguration configuration,
        ILogger<SummaryPoster> logger)
    {
        _summaryGenerator = summaryGenerator ?? throw new ArgumentNullException(nameof(summaryGenerator));
        _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
        _workspaceRepository = workspaceRepository ?? throw new ArgumentNullException(nameof(workspaceRepository));
        _telegramClient = telegramClient ?? throw new ArgumentNullException(nameof(telegramClient));
        _whatsAppClient = whatsAppClient ?? throw new ArgumentNullException(nameof(whatsAppClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> PostSummaryAsync(
        Guid tenantId,
        Guid channelId,
        SummaryPeriod period,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Posting {Period} summary for channel {ChannelId}, tenant {TenantId}",
                period, channelId, tenantId);

            // Get channel
            var channel = await _channelRepository.GetByIdAsync(channelId, tenantId, cancellationToken);
            if (channel == null)
            {
                _logger.LogWarning("Channel {ChannelId} not found for tenant {TenantId}", channelId, tenantId);
                return false;
            }

            // Get workspace to determine platform
            var workspace = await _workspaceRepository.GetByIdAsync(channel.WorkspaceId, tenantId, cancellationToken);
            if (workspace == null)
            {
                _logger.LogWarning("Workspace {WorkspaceId} not found for channel {ChannelId}",
                    channel.WorkspaceId, channelId);
                return false;
            }

            // Generate summary
            var summary = await _summaryGenerator.GenerateSummaryAsync(
                tenantId,
                channelId,
                workspace.Platform,
                period,
                includeBranding: true,
                cancellationToken: cancellationToken);

            // Post based on platform
            bool success = workspace.Platform switch
            {
                Platform.Telegram => await PostToTelegramAsync(tenantId, channel.ExternalId, summary, cancellationToken),
                Platform.WhatsApp => await PostToWhatsAppAsync(tenantId, channel.ExternalId, summary, cancellationToken),
                _ => throw new NotSupportedException($"Platform {workspace.Platform} is not supported for summary posting")
            };

            if (success)
            {
                _logger.LogInformation("Successfully posted {Period} summary to channel {ChannelId}",
                    period, channelId);
            }
            else
            {
                _logger.LogWarning("Failed to post {Period} summary to channel {ChannelId}",
                    period, channelId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting summary for channel {ChannelId}", channelId);
            return false;
        }
    }

    private async Task<bool> PostToTelegramAsync(
        Guid tenantId,
        string chatId,
        string summary,
        CancellationToken cancellationToken)
    {
        // Get bot token from configuration
        // Format: Platforms:Telegram:BotTokens:{tenantId} or Platforms:Telegram:DefaultBotToken
        var botToken = _configuration[$"Platforms:Telegram:BotTokens:{tenantId}"]
            ?? _configuration["Platforms:Telegram:DefaultBotToken"];

        if (string.IsNullOrEmpty(botToken))
        {
            _logger.LogError("No Telegram bot token configured for tenant {TenantId}", tenantId);
            return false;
        }

        return await _telegramClient.SendMessageAsync(
            botToken,
            chatId,
            summary,
            parseMarkdown: true,
            cancellationToken);
    }

    private Task<bool> PostToWhatsAppAsync(
        Guid tenantId,
        string recipientPhone,
        string summary,
        CancellationToken cancellationToken)
    {
        // Get WhatsApp credentials from configuration
        var accessToken = _configuration[$"Platforms:WhatsApp:AccessTokens:{tenantId}"]
            ?? _configuration["Platforms:WhatsApp:DefaultAccessToken"];

        var phoneNumberId = _configuration[$"Platforms:WhatsApp:PhoneNumberIds:{tenantId}"]
            ?? _configuration["Platforms:WhatsApp:DefaultPhoneNumberId"];

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(phoneNumberId))
        {
            _logger.LogError("WhatsApp credentials not configured for tenant {TenantId}", tenantId);
            return Task.FromResult(false);
        }

        // Extract actual phone number from channel external ID (format: wa:{hashed-phone})
        // For WhatsApp we need the original phone number, but we only stored the hash
        // This is a limitation - we need to store the original phone number securely
        // For MVP, we'll need to pass the phone number from a different source
        // TODO: Store encrypted phone numbers for WhatsApp summary posting

        _logger.LogWarning("WhatsApp summary posting not fully implemented - requires phone number storage");
        return Task.FromResult(false);
    }
}