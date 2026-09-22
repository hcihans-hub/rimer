namespace RimerApi.Domain.Entities;

/// <summary>
/// Records a single change event in a ticket's lifecycle for audit purposes.
/// References users by Guid only — user management is handled by ASP.NET Identity.
/// </summary>
public class TicketHistory : BaseEntity
{
    /// <summary>Id of the ticket this history entry belongs to.</summary>
    public Guid TicketId { get; set; }

    /// <summary>Description of the action performed (e.g. "StatusChanged", "Assigned", "Created").</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Previous value before the change (null for creation events).</summary>
    public string? OldValue { get; set; }

    /// <summary>New value after the change.</summary>
    public string? NewValue { get; set; }

    /// <summary>Id of the user who made this change.</summary>
    public Guid ChangedById { get; set; }

    /// <summary>Name of the department when this change occurred.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Optional note or comment regarding this change.</summary>
    public string? Note { get; set; }

    /// <summary>UTC timestamp when the change occurred.</summary>
    public DateTime ChangedAt { get; set; }

    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>The ticket this history entry belongs to.</summary>
    public Ticket Ticket { get; set; } = null!;
}
