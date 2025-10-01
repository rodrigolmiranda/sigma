using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Analytics;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.Infrastructure.Persistence.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly SigmaDbContext _context;

    public AnalyticsRepository(SigmaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<EngagementSummary>> GetEngagementSummaryAsync(
        Guid tenantId,
        Guid? channelId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Database.SqlQuery<EngagementSummary>(
            $@"SELECT
                channel_id as {nameof(EngagementSummary.ChannelId)},
                tenant_id as {nameof(EngagementSummary.TenantId)},
                workspace_id as {nameof(EngagementSummary.WorkspaceId)},
                channel_name as {nameof(EngagementSummary.ChannelName)},
                period_date as {nameof(EngagementSummary.PeriodDate)},
                message_count as {nameof(EngagementSummary.MessageCount)},
                active_member_count as {nameof(EngagementSummary.ActiveMemberCount)},
                text_message_count as {nameof(EngagementSummary.TextMessageCount)},
                image_count as {nameof(EngagementSummary.ImageCount)},
                file_count as {nameof(EngagementSummary.FileCount)},
                reply_count as {nameof(EngagementSummary.ReplyCount)},
                first_message_at as {nameof(EngagementSummary.FirstMessageAt)},
                last_message_at as {nameof(EngagementSummary.LastMessageAt)}
            FROM vw_engagement_summary
            WHERE tenant_id = {tenantId}"
        );

        if (channelId.HasValue)
        {
            query = _context.Database.SqlQuery<EngagementSummary>(
                $@"SELECT
                    channel_id as {nameof(EngagementSummary.ChannelId)},
                    tenant_id as {nameof(EngagementSummary.TenantId)},
                    workspace_id as {nameof(EngagementSummary.WorkspaceId)},
                    channel_name as {nameof(EngagementSummary.ChannelName)},
                    period_date as {nameof(EngagementSummary.PeriodDate)},
                    message_count as {nameof(EngagementSummary.MessageCount)},
                    active_member_count as {nameof(EngagementSummary.ActiveMemberCount)},
                    text_message_count as {nameof(EngagementSummary.TextMessageCount)},
                    image_count as {nameof(EngagementSummary.ImageCount)},
                    file_count as {nameof(EngagementSummary.FileCount)},
                    reply_count as {nameof(EngagementSummary.ReplyCount)},
                    first_message_at as {nameof(EngagementSummary.FirstMessageAt)},
                    last_message_at as {nameof(EngagementSummary.LastMessageAt)}
                FROM vw_engagement_summary
                WHERE tenant_id = {tenantId} AND channel_id = {channelId.Value}"
            );
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.PeriodDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.PeriodDate <= endDate.Value);
        }

        return await query
            .OrderBy(e => e.PeriodDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TopContributor>> GetTopContributorsAsync(
        Guid tenantId,
        Guid channelId,
        int limit = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Database.SqlQuery<TopContributor>(
            $@"SELECT
                channel_id as {nameof(TopContributor.ChannelId)},
                tenant_id as {nameof(TopContributor.TenantId)},
                sender_platform_user_id as {nameof(TopContributor.SenderPlatformUserId)},
                sender_display_name as {nameof(TopContributor.SenderDisplayName)},
                message_count as {nameof(TopContributor.MessageCount)},
                active_days as {nameof(TopContributor.ActiveDays)},
                first_message_at as {nameof(TopContributor.FirstMessageAt)},
                last_message_at as {nameof(TopContributor.LastMessageAt)},
                rank as {nameof(TopContributor.Rank)}
            FROM vw_top_contributors
            WHERE tenant_id = {tenantId} AND channel_id = {channelId}"
        );

        if (startDate.HasValue)
        {
            query = query.Where(c => c.FirstMessageAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(c => c.LastMessageAt <= endDate.Value);
        }

        return await query
            .OrderBy(c => c.Rank)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PeriodComparison>> GetPeriodComparisonAsync(
        Guid tenantId,
        Guid channelId,
        PeriodType periodType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        // Convert enum to lowercase string for database query
        var periodTypeString = periodType.ToString().ToLower();

        var query = _context.Database.SqlQuery<PeriodComparison>(
            $@"SELECT
                channel_id as {nameof(PeriodComparison.ChannelId)},
                tenant_id as {nameof(PeriodComparison.TenantId)},
                period_type as {nameof(PeriodComparison.PeriodType)},
                period_date as {nameof(PeriodComparison.PeriodDate)},
                message_count as {nameof(PeriodComparison.MessageCount)},
                active_member_count as {nameof(PeriodComparison.ActiveMemberCount)}
            FROM vw_period_comparison
            WHERE tenant_id = {tenantId} AND channel_id = {channelId} AND period_type = {periodTypeString}"
        );

        if (startDate.HasValue)
        {
            query = query.Where(p => p.PeriodDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.PeriodDate <= endDate.Value);
        }

        return await query
            .OrderBy(p => p.PeriodDate)
            .ToListAsync(cancellationToken);
    }
}