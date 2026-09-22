namespace RimerApi.Domain.Entities;

/// <summary>
/// Represents a university department that tickets can be routed to.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>Name of the department.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the department's responsibilities.</summary>
    public string Description { get; set; } = string.Empty;


    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>Tickets initially routed to this department.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    /// <summary>Tickets currently assigned to this department (authoritative).</summary>
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}
