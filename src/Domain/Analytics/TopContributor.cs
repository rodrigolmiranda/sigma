namespace Sigma.Domain.Analytics;

public class TopContributor
{
    public Guid ChannelId { get; set; }
    public Guid TenantId { get; set; }
    public string SenderPlatformUserId { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public int ActiveDays { get; set; }
    public DateTime FirstMessageAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public long Rank { get; set; }
}