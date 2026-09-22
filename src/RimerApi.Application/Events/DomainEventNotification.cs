using MediatR;

namespace RimerApi.Application.Events;

/// <summary>
/// Generic wrapper to translate domain events into MediatR notifications
/// without coupling the Domain layer to MediatR.
/// </summary>
public class DomainEventNotification<TDomainEvent> : INotification
{
    public TDomainEvent DomainEvent { get; }

    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
