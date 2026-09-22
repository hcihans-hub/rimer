using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Extensions;
using RimerApi.API.Models;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Notification;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets recent notifications for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications()
    {
        var notifications = await _notificationService.GetMyNotificationsAsync(User.GetCurrentUser());
        return Ok(ApiResponse<IEnumerable<NotificationDto>>.Ok(notifications));
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(User.GetCurrentUser());
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    [HttpPost("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notificationService.MarkReadAsync(id, User.GetCurrentUser());
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Marks all notifications for the current user as read.
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notificationService.MarkAllReadAsync(User.GetCurrentUser());
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
