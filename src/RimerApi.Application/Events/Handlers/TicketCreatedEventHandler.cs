using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Events;

namespace RimerApi.Application.Events.Handlers;

public class TicketCreatedEventHandler : INotificationHandler<DomainEventNotification<TicketCreatedEvent>>
{
    private readonly IBackgroundJobService _jobService;
    private readonly ILogger<TicketCreatedEventHandler> _logger;

    public TicketCreatedEventHandler(
        IBackgroundJobService jobService, 
        ILogger<TicketCreatedEventHandler> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<TicketCreatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        
        _logger.LogInformation("Enqueuing background job for TicketCreatedEvent {TicketId}", domainEvent.TicketId);

        _jobService.Enqueue<IEventBackgroundWorker>(w => 
            w.ProcessTicketCreatedAsync(domainEvent.TicketId, domainEvent.CreatorId, domainEvent.Title));

        return Task.CompletedTask;
    }
}
