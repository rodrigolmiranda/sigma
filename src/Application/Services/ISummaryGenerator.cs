using Sigma.Shared.Enums;

namespace Sigma.Application.Services;

public interface ISummaryGenerator
{
    Task<string> GenerateSummaryAsync(
        Guid tenantId,
        Guid channelId,
        Platform platform,
        SummaryPeriod period,
        DateTime? endDate = null,
        bool includeBranding = true,
        CancellationToken cancellationToken = default);
}