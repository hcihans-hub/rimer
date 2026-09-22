namespace RimerApi.Application.DTOs.Ticket;

public class TicketReminderDto
{
    public int Id { get; set; }
    public Guid TicketId { get; set; }
    public string TicketTitle { get; set; } = string.Empty;
    public string TicketReferenceNo { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ReminderAt { get; set; }
    public bool IsDismissed { get; set; }
    public DateTime CreatedAt { get; set; }
}
