namespace RimerApi.Domain.Entities;

/// <summary>
/// In-app notification sent to a specific user.
/// Events: new ticket → dept users, transfer → new dept, reply/close → ticket owner.
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>Id of the user who receives this notification.</summary>
    public Guid UserId { get; set; }

    /// <summary>Notification message text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Whether the user has read this notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>Optional reference to the related ticket.</summary>
    public Guid? TicketId { get; set; }

    /// <summary>Notification type tag (e.g. "NewTicket", "Transfer", "Reply", "Close").</summary>
    public string? Type { get; set; }
}
