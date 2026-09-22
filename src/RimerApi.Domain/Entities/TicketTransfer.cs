namespace RimerApi.Domain.Entities;

/// <summary>
/// Records a single department transfer event for a ticket.
/// The old department loses visibility; the new one gains it.
/// </summary>
public class TicketTransfer : BaseEntity
{
    /// <summary>Id of the ticket that was transferred.</summary>
    public Guid TicketId { get; set; }

    /// <summary>Department the ticket was transferred FROM.</summary>
    public Guid FromDepartmentId { get; set; }

    /// <summary>Department the ticket was transferred TO.</summary>
    public Guid ToDepartmentId { get; set; }

    /// <summary>Optional note explaining why the transfer was made.</summary>
    public string? Note { get; set; }

    /// <summary>Id of the user who initiated the transfer.</summary>
    public Guid TransferredById { get; set; }

    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>The ticket this transfer belongs to.</summary>
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Department the ticket was transferred from.</summary>
    public Department? FromDepartment { get; set; }

    /// <summary>Department the ticket was transferred to.</summary>
    public Department? ToDepartment { get; set; }
}
