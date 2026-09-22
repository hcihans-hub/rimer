using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Events;

namespace RimerApi.Application.Events.Handlers;

public class TicketAssignedEventHandler : INotificationHandler<DomainEventNotification<TicketAssignedEvent>>
{
    private readonly IBackgroundJobService _jobService;
    private readonly ILogger<TicketAssignedEventHandler> _logger;

    public TicketAssignedEventHandler(
        IBackgroundJobService jobService, 
        ILogger<TicketAssignedEventHandler> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<TicketAssignedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation("Enqueuing background job for TicketAssignedEvent {TicketId}", domainEvent.TicketId);

        _jobService.Enqueue<IEventBackgroundWorker>(w => 
            w.ProcessTicketAssignedAsync(domainEvent.TicketId, domainEvent.AssignedToId));

        return Task.CompletedTask;
    }
}
