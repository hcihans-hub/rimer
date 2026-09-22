using System;

namespace RimerApi.Domain.Events;

/// <summary>
/// Domain event published when a ticket is assigned to a specific user.
/// </summary>
public record TicketAssignedEvent(Guid TicketId, Guid AssignedToId);
