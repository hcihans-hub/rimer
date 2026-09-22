using System;

namespace RimerApi.Domain.Events;

/// <summary>
/// Domain event published when a ticket's status is changed to Closed.
/// </summary>
public record TicketClosedEvent(Guid TicketId, Guid CloserId, Guid? CreatorId);
