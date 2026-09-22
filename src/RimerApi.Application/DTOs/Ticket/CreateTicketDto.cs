using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// DTO for creating a new ticket.
/// </summary>
public class CreateTicketDto
{
    /// <summary>Short title summarizing the ticket subject.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed description of the ticket.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Category classifying the ticket type.</summary>
    public TicketCategory Category { get; set; }

    /// <summary>Priority level (1=Low, 2=Normal, 3=Important, 4=Critical).</summary>
    public int Priority { get; set; } = 2;

    /// <summary>Optional department to route the ticket to.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Institution the ticket is addressed to.</summary>
    public string? InstitutionName { get; set; }

    /// <summary>Whether user accepted the terms.</summary>
    public bool TermsAccepted { get; set; }
}
