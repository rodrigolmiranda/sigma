using Sigma.Shared.Enums;

namespace Sigma.Application.Services;

public interface ISummaryPoster
{
    Task<bool> PostSummaryAsync(
        Guid tenantId,
        Guid channelId,
        SummaryPeriod period,
        CancellationToken cancellationToken = default);
}