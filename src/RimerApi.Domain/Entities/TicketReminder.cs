namespace RimerApi.Domain.Entities;

public class TicketReminder
{
    public int Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public string? Note { get; set; }
    public DateTime ReminderAt { get; set; }
    public bool IsDismissed { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
}
