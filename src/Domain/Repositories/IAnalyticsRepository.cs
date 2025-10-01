using Sigma.Domain.Analytics;
using Sigma.Shared.Enums;

namespace Sigma.Domain.Repositories;

public interface IAnalyticsRepository
{
    Task<IEnumerable<EngagementSummary>> GetEngagementSummaryAsync(
        Guid tenantId,
        Guid? channelId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TopContributor>> GetTopContributorsAsync(
        Guid tenantId,
        Guid channelId,
        int limit = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PeriodComparison>> GetPeriodComparisonAsync(
        Guid tenantId,
        Guid channelId,
        PeriodType periodType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}