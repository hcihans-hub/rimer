namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Request body for transferring a ticket to another department.
/// </summary>
public class TransferTicketDto
{
    /// <summary>Id of the target department.</summary>
    public Guid ToDepartmentId { get; set; }

    /// <summary>Optional explanation note for the transfer.</summary>
    public string? Note { get; set; }
}
