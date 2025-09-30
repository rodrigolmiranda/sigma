using Sigma.Domain.Common;

namespace Sigma.Application.Contracts;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}