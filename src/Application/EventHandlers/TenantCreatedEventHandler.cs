using Microsoft.Extensions.Logging;
using Sigma.Application.Contracts;
using Sigma.Domain.Events;

namespace Sigma.Application.EventHandlers;

public class TenantCreatedEventHandler : IDomainEventHandler<TenantCreatedEvent>
{
    private readonly ILogger<TenantCreatedEventHandler> _logger;

    public TenantCreatedEventHandler(ILogger<TenantCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TenantCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant created event received - TenantId: {TenantId}, Name: {Name}, Slug: {Slug}, Plan: {PlanType}",
            domainEvent.TenantId,
            domainEvent.Name,
            domainEvent.Slug,
            domainEvent.PlanType);

        // TODO: Send welcome email, create default workspace, etc.

        return Task.CompletedTask;
    }
}