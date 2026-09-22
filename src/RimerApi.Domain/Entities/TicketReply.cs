namespace RimerApi.Domain.Entities;

/// <summary>
/// A reply added by a UnitUser or Staff member to a ticket.
/// The reply is visible to the ticket owner.
/// </summary>
public class TicketReply : BaseEntity
{
    /// <summary>Id of the ticket this reply belongs to.</summary>
    public Guid TicketId { get; set; }

    /// <summary>Id of the user who wrote the reply.</summary>
    public Guid AuthorId { get; set; }

    /// <summary>Reply message content.</summary>
    public string Message { get; set; } = string.Empty;

    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>The ticket this reply is attached to.</summary>
    public Ticket Ticket { get; set; } = null!;
}
