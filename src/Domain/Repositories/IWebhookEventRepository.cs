using Sigma.Domain.Entities;

namespace Sigma.Domain.Repositories;

public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByPlatformAndEventIdAsync(
        string platform,
        string eventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}