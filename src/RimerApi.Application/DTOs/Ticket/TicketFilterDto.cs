using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// DTO for filtering and paginating the ticket list.
/// </summary>
public class TicketFilterDto
{
    /// <summary>Filter by ticket status.</summary>
    public TicketStatus? Status { get; set; }

    /// <summary>Filter by ticket category.</summary>
    public TicketCategory? Category { get; set; }

    /// <summary>Filter by department id.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Filter by creator user id.</summary>
    public Guid? CreatorId { get; set; }

    /// <summary>Filter by assigned staff user id.</summary>
    public Guid? AssignedToId { get; set; }

    /// <summary>Search term to match against title or description.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Number of items per page. Defaults to 10.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Flag to exclude closed tickets from the result.</summary>
    public bool ExcludeClosed { get; set; } = false;

    /// <summary>Filter by start date.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Filter by end date.</summary>
    public DateTime? EndDate { get; set; }
}
