using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Events;

namespace RimerApi.Application.Events.Handlers;

public class TicketClosedEventHandler : INotificationHandler<DomainEventNotification<TicketClosedEvent>>
{
    private readonly IBackgroundJobService _jobService;
    private readonly ILogger<TicketClosedEventHandler> _logger;

    public TicketClosedEventHandler(
        IBackgroundJobService jobService, 
        ILogger<TicketClosedEventHandler> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<TicketClosedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Enqueuing background job for TicketClosedEvent {TicketId}", domainEvent.TicketId);

        _jobService.Enqueue<IEventBackgroundWorker>(w => 
            w.ProcessTicketClosedAsync(domainEvent.TicketId, domainEvent.CreatorId));

        return Task.CompletedTask;
    }
}
