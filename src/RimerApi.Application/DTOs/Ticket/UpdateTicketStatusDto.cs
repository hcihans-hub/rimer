using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// DTO for updating a ticket's status.
/// </summary>
public class UpdateTicketStatusDto
{
    /// <summary>The new status to set.</summary>
    public TicketStatus Status { get; set; }

    /// <summary>Optional note explaining the status change.</summary>
    public string? Note { get; set; }
}
