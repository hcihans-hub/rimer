using System;

namespace RimerApi.Domain.Events;

/// <summary>
/// Domain event published when a new ticket is created.
/// </summary>
public record TicketCreatedEvent(Guid TicketId, Guid? CreatorId, string Title);
