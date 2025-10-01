using System.Text;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.Application.Services;

public class SummaryGenerator : ISummaryGenerator
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IChannelRepository _channelRepository;

    public SummaryGenerator(
        IAnalyticsRepository analyticsRepository,
        IChannelRepository channelRepository)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
    }

    public async Task<string> GenerateSummaryAsync(
        Guid tenantId,
        Guid channelId,
        Platform platform,
        SummaryPeriod period,
        DateTime? endDate = null,
        bool includeBranding = true,
        CancellationToken cancellationToken = default)
    {
        endDate ??= DateTime.UtcNow;

        // Calculate start date based on period
        var startDate = period switch
        {
            SummaryPeriod.Daily => endDate.Value.Date.AddDays(-1),
            SummaryPeriod.Weekly => endDate.Value.Date.AddDays(-7),
            SummaryPeriod.Monthly => endDate.Value.Date.AddMonths(-1),
            _ => endDate.Value.Date.AddDays(-1)
        };

        // Get channel info
        var channel = await _channelRepository.GetByIdAsync(channelId, tenantId, cancellationToken);
        if (channel == null)
        {
            throw new InvalidOperationException($"Channel {channelId} not found");
        }

        // Get analytics data
        var engagement = (await _analyticsRepository.GetEngagementSummaryAsync(
            tenantId,
            channelId,
            startDate,
            endDate,
            cancellationToken)).ToList();

        var topContributors = (await _analyticsRepository.GetTopContributorsAsync(
            tenantId,
            channelId,
            limit: 5,
            startDate: startDate,
            endDate: endDate,
            cancellationToken)).ToList();

        // Calculate totals
        var totalMessages = engagement.Sum(e => e.MessageCount);
        var totalMembers = engagement.Max(e => e.ActiveMemberCount);
        var totalReplies = engagement.Sum(e => e.ReplyCount);
        var totalImages = engagement.Sum(e => e.ImageCount);
        var totalFiles = engagement.Sum(e => e.FileCount);

        // Format summary based on platform
        return platform switch
        {
            Platform.Telegram => FormatTelegramSummary(channel.Name, period, startDate, endDate.Value, totalMessages, totalMembers, totalReplies, totalImages, totalFiles, topContributors, includeBranding),
            Platform.WhatsApp => FormatWhatsAppSummary(channel.Name, period, startDate, endDate.Value, totalMessages, totalMembers, totalReplies, totalImages, totalFiles, topContributors, includeBranding),
            _ => throw new NotSupportedException($"Platform {platform} is not supported for summary generation")
        };
    }

    private string FormatTelegramSummary(
        string channelName,
        SummaryPeriod period,
        DateTime startDate,
        DateTime endDate,
        int totalMessages,
        int totalMembers,
        int totalReplies,
        int totalImages,
        int totalFiles,
        List<Domain.Analytics.TopContributor> topContributors,
        bool includeBranding)
    {
        var sb = new StringBuilder();

        // Header with emoji
        sb.AppendLine($"📊 **{channelName} - {FormatPeriodName(period)} Summary**");
        sb.AppendLine($"📅 {startDate:MMM dd} - {endDate:MMM dd, yyyy}");
        sb.AppendLine();

        // Key stats
        sb.AppendLine("**📈 Activity:**");
        sb.AppendLine($"• 💬 {totalMessages:N0} messages");
        sb.AppendLine($"• 👥 {totalMembers:N0} active members");
        if (totalReplies > 0)
            sb.AppendLine($"• 💭 {totalReplies:N0} replies");
        if (totalImages > 0)
            sb.AppendLine($"• 🖼️ {totalImages:N0} images");
        if (totalFiles > 0)
            sb.AppendLine($"• 📎 {totalFiles:N0} files");
        sb.AppendLine();

        // Top contributors
        if (topContributors.Any())
        {
            sb.AppendLine("**🏆 Top Contributors:**");
            var medals = new[] { "🥇", "🥈", "🥉", "4️⃣", "5️⃣" };
            for (int i = 0; i < Math.Min(topContributors.Count, 5); i++)
            {
                var contributor = topContributors[i];
                sb.AppendLine($"{medals[i]} {contributor.SenderDisplayName} - {contributor.MessageCount:N0} messages");
            }
            sb.AppendLine();
        }

        // Branding
        if (includeBranding)
        {
            sb.AppendLine("---");
            sb.AppendLine("_Powered by_ [SIGMA](https://sigma.chat) 🚀");
            sb.AppendLine("_Track your group's engagement and insights_");
        }

        return sb.ToString();
    }

    private string FormatWhatsAppSummary(
        string channelName,
        SummaryPeriod period,
        DateTime startDate,
        DateTime endDate,
        int totalMessages,
        int totalMembers,
        int totalReplies,
        int totalImages,
        int totalFiles,
        List<Domain.Analytics.TopContributor> topContributors,
        bool includeBranding)
    {
        var sb = new StringBuilder();

        // Header (WhatsApp uses plain text, but emojis work)
        sb.AppendLine($"📊 *{channelName} - {FormatPeriodName(period)} Summary*");
        sb.AppendLine($"📅 {startDate:MMM dd} - {endDate:MMM dd, yyyy}");
        sb.AppendLine();

        // Key stats
        sb.AppendLine("*📈 Activity:*");
        sb.AppendLine($"• 💬 {totalMessages:N0} messages");
        sb.AppendLine($"• 👥 {totalMembers:N0} active members");
        if (totalReplies > 0)
            sb.AppendLine($"• 💭 {totalReplies:N0} replies");
        if (totalImages > 0)
            sb.AppendLine($"• 🖼️ {totalImages:N0} images");
        if (totalFiles > 0)
            sb.AppendLine($"• 📎 {totalFiles:N0} files");
        sb.AppendLine();

        // Top contributors
        if (topContributors.Any())
        {
            sb.AppendLine("*🏆 Top Contributors:*");
            var medals = new[] { "🥇", "🥈", "🥉", "4️⃣", "5️⃣" };
            for (int i = 0; i < Math.Min(topContributors.Count, 5); i++)
            {
                var contributor = topContributors[i];
                sb.AppendLine($"{medals[i]} {contributor.SenderDisplayName} - {contributor.MessageCount:N0} messages");
            }
            sb.AppendLine();
        }

        // Branding (WhatsApp doesn't support links, use plain text)
        if (includeBranding)
        {
            sb.AppendLine("━━━━━━━━━━━━━━━");
            sb.AppendLine("_Powered by SIGMA_ 🚀");
            sb.AppendLine("_Track your group's engagement and insights_");
            sb.AppendLine("Visit: sigma.chat");
        }

        return sb.ToString();
    }

    private string FormatPeriodName(SummaryPeriod period)
    {
        return period switch
        {
            SummaryPeriod.Daily => "Daily",
            SummaryPeriod.Weekly => "Weekly",
            SummaryPeriod.Monthly => "Monthly",
            _ => "Daily"
        };
    }
}