using Microsoft.EntityFrameworkCore;
using Sigma.Domain.Entities;
using Sigma.Domain.Repositories;

namespace Sigma.Infrastructure.Persistence.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly SigmaDbContext _context;

    public WebhookEventRepository(SigmaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<WebhookEvent?> GetByPlatformAndEventIdAsync(
        string platform,
        string eventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .FirstOrDefaultAsync(
                we => we.Platform == platform && we.EventId == eventId,
                cancellationToken);
    }

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await _context.WebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public async Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _context.WebhookEvents.Update(webhookEvent);
        await Task.CompletedTask;
    }
}