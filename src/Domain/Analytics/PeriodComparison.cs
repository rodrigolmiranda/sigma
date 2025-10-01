namespace Sigma.Domain.Analytics;

public class PeriodComparison
{
    public Guid ChannelId { get; set; }
    public Guid TenantId { get; set; }
    public string PeriodType { get; set; } = string.Empty; // "day", "week", "month"
    public DateTime PeriodDate { get; set; }
    public int MessageCount { get; set; }
    public int ActiveMemberCount { get; set; }
}