namespace Sigma.Shared.Enums;

/// <summary>
/// Represents the period for automated summary generation and posting.
/// </summary>
public enum SummaryPeriod
{
    /// <summary>
    /// Daily summary (covers last 24 hours).
    /// </summary>
    Daily,

    /// <summary>
    /// Weekly summary (covers last 7 days).
    /// </summary>
    Weekly,

    /// <summary>
    /// Monthly summary (covers last 30 days).
    /// </summary>
    Monthly
}
