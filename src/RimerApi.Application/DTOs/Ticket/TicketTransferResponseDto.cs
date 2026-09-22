namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Response DTO for a TicketTransfer record.
/// </summary>
public class TicketTransferResponseDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string FromDepartmentName { get; set; } = string.Empty;
    public string ToDepartmentName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
