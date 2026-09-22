using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Notification;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Service for creating, querying and marking notifications.
/// </summary>
public interface INotificationService
{
    /// <summary>Get all notifications for the current user (newest first).</summary>
    Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(CurrentUser currentUser);

    /// <summary>Get the unread notification count for the current user.</summary>
    Task<int> GetUnreadCountAsync(CurrentUser currentUser);

    /// <summary>Mark a single notification as read.</summary>
    Task MarkReadAsync(Guid notificationId, CurrentUser currentUser);

    /// <summary>Mark all notifications for the current user as read.</summary>
    Task MarkAllReadAsync(CurrentUser currentUser);

    /// <summary>
    /// Create a notification for a specific user.
    /// Called internally by TicketService on events (new ticket, transfer, reply, close).
    /// </summary>
    Task CreateAsync(Guid userId, string message, Guid? ticketId = null, string? type = null);

    /// <summary>
    /// Notify all UnitUsers in the given department.
    /// </summary>
    Task NotifyDepartmentUsersAsync(Guid departmentId, string message, Guid? ticketId = null, string? type = null);
}
