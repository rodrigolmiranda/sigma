using HotChocolate.Authorization;
using Sigma.Domain.Analytics;
using Sigma.Domain.Repositories;
using Sigma.Shared.Enums;

namespace Sigma.API.GraphQL;

[ExtendObjectType(typeof(Query))]
public class AnalyticsQueries
{
    [Authorize]
    public async Task<IEnumerable<EngagementSummary>> GetEngagementSummary(
        Guid tenantId,
        Guid? channelId,
        DateTime? startDate,
        DateTime? endDate,
        [Service] IAnalyticsRepository analyticsRepository,
        CancellationToken cancellationToken)
    {
        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("startDate must be less than or equal to endDate");
        }

        return await analyticsRepository.GetEngagementSummaryAsync(
            tenantId,
            channelId,
            startDate,
            endDate,
            cancellationToken);
    }

    [Authorize]
    public async Task<IEnumerable<TopContributor>> GetTopContributors(
        Guid tenantId,
        Guid channelId,
        int limit = 10,
        DateTime? startDate = null,
        DateTime? endDate = null,
        [Service] IAnalyticsRepository analyticsRepository = null!,
        CancellationToken cancellationToken = default)
    {
        // Validate limit parameter
        if (limit < 1 || limit > 100)
        {
            throw new ArgumentException("limit must be between 1 and 100", nameof(limit));
        }

        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("startDate must be less than or equal to endDate");
        }

        return await analyticsRepository.GetTopContributorsAsync(
            tenantId,
            channelId,
            limit,
            startDate,
            endDate,
            cancellationToken);
    }

    [Authorize]
    public async Task<IEnumerable<PeriodComparison>> GetPeriodComparison(
        Guid tenantId,
        Guid channelId,
        PeriodType periodType,
        DateTime? startDate,
        DateTime? endDate,
        [Service] IAnalyticsRepository analyticsRepository,
        CancellationToken cancellationToken)
    {
        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ArgumentException("startDate must be less than or equal to endDate");
        }

        return await analyticsRepository.GetPeriodComparisonAsync(
            tenantId,
            channelId,
            periodType,
            startDate,
            endDate,
            cancellationToken);
    }
}