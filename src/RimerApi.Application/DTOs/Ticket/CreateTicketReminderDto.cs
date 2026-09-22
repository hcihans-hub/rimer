namespace RimerApi.Application.DTOs.Ticket;

public class CreateTicketReminderDto
{
    public DateTime ReminderAt { get; set; }
    public string? Note { get; set; }
}
