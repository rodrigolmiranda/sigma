using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // View 1: Engagement Summary - Aggregated metrics per channel and time period
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_engagement_summary AS
                SELECT
                    m.channel_id,
                    m.tenant_id,
                    c.workspace_id,
                    c.name as channel_name,
                    DATE_TRUNC('day', m.timestamp_utc) as period_date,
                    COUNT(*) as message_count,
                    COUNT(DISTINCT m.sender_platform_user_id) as active_member_count,
                    COUNT(*) FILTER (WHERE m.type = 'Text') as text_message_count,
                    COUNT(*) FILTER (WHERE m.type = 'Image') as image_count,
                    COUNT(*) FILTER (WHERE m.type = 'File') as file_count,
                    COUNT(DISTINCT CASE WHEN m.reply_to_platform_message_id IS NOT NULL THEN m.id END) as reply_count,
                    MIN(m.timestamp_utc) as first_message_at,
                    MAX(m.timestamp_utc) as last_message_at
                FROM messages m
                INNER JOIN channels c ON m.channel_id = c.id
                WHERE m.is_deleted = FALSE
                GROUP BY m.channel_id, m.tenant_id, c.workspace_id, c.name, DATE_TRUNC('day', m.timestamp_utc)
            ");

            // View 2: Top Contributors - Ranked senders by activity
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_top_contributors AS
                SELECT
                    m.channel_id,
                    m.tenant_id,
                    m.sender_platform_user_id,
                    m.sender_display_name,
                    COUNT(*) as message_count,
                    COUNT(DISTINCT DATE_TRUNC('day', m.timestamp_utc)) as active_days,
                    MIN(m.timestamp_utc) as first_message_at,
                    MAX(m.timestamp_utc) as last_message_at,
                    ROW_NUMBER() OVER (
                        PARTITION BY m.channel_id, m.tenant_id
                        ORDER BY COUNT(*) DESC
                    ) as rank
                FROM messages m
                WHERE m.is_deleted = FALSE
                  AND m.sender_is_bot = FALSE
                GROUP BY m.channel_id, m.tenant_id, m.sender_platform_user_id, m.sender_display_name
            ");

            // View 3: Period Comparison - Daily aggregations for trending
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_period_comparison AS
                WITH daily_stats AS (
                    SELECT
                        m.channel_id,
                        m.tenant_id,
                        DATE_TRUNC('day', m.timestamp_utc) as period_date,
                        COUNT(*) as message_count,
                        COUNT(DISTINCT m.sender_platform_user_id) as active_member_count
                    FROM messages m
                    WHERE m.is_deleted = FALSE
                    GROUP BY m.channel_id, m.tenant_id, DATE_TRUNC('day', m.timestamp_utc)
                ),
                weekly_stats AS (
                    SELECT
                        m.channel_id,
                        m.tenant_id,
                        DATE_TRUNC('week', m.timestamp_utc) as period_date,
                        COUNT(*) as message_count,
                        COUNT(DISTINCT m.sender_platform_user_id) as active_member_count
                    FROM messages m
                    WHERE m.is_deleted = FALSE
                    GROUP BY m.channel_id, m.tenant_id, DATE_TRUNC('week', m.timestamp_utc)
                ),
                monthly_stats AS (
                    SELECT
                        m.channel_id,
                        m.tenant_id,
                        DATE_TRUNC('month', m.timestamp_utc) as period_date,
                        COUNT(*) as message_count,
                        COUNT(DISTINCT m.sender_platform_user_id) as active_member_count
                    FROM messages m
                    WHERE m.is_deleted = FALSE
                    GROUP BY m.channel_id, m.tenant_id, DATE_TRUNC('month', m.timestamp_utc)
                )
                SELECT channel_id, tenant_id, 'day' as period_type, period_date, message_count, active_member_count FROM daily_stats
                UNION ALL
                SELECT channel_id, tenant_id, 'week' as period_type, period_date, message_count, active_member_count FROM weekly_stats
                UNION ALL
                SELECT channel_id, tenant_id, 'month' as period_type, period_date, message_count, active_member_count FROM monthly_stats
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_period_comparison");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_top_contributors");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_engagement_summary");
        }
    }
}
