namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// DTO for assigning a ticket to a staff member and/or department.
/// </summary>
public class AssignTicketDto
{
    /// <summary>Id of the staff member to assign the ticket to.</summary>
    public Guid? AssignedToId { get; set; }

    /// <summary>Id of the department to route the ticket to.</summary>
    public Guid? DepartmentId { get; set; }
}
