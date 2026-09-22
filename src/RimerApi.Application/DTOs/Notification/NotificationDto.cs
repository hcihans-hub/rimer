namespace RimerApi.Application.DTOs.Notification;

/// <summary>
/// Response DTO for a user notification.
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public Guid? TicketId { get; set; }
    public string? Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
