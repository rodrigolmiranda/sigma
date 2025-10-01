namespace Sigma.Domain.Analytics;

public class EngagementSummary
{
    public Guid ChannelId { get; set; }
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public DateTime PeriodDate { get; set; }
    public int MessageCount { get; set; }
    public int ActiveMemberCount { get; set; }
    public int TextMessageCount { get; set; }
    public int ImageCount { get; set; }
    public int FileCount { get; set; }
    public int ReplyCount { get; set; }
    public DateTime FirstMessageAt { get; set; }
    public DateTime LastMessageAt { get; set; }
}