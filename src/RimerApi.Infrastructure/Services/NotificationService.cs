using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Notification;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Infrastructure.Data;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Concrete implementation of INotificationService backed by ApplicationDbContext.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(CurrentUser currentUser)
    {
        return await _db.Notifications
            .Where(n => n.UserId == currentUser.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                TicketId = n.TicketId,
                Type = n.Type,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(CurrentUser currentUser)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == currentUser.Id && !n.IsRead);
    }

    /// <inheritdoc />
    public async Task MarkReadAsync(Guid notificationId, CurrentUser currentUser)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == currentUser.Id);

        if (notification is not null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task MarkAllReadAsync(CurrentUser currentUser)
    {
        await _db.Notifications
            .Where(n => n.UserId == currentUser.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    /// <inheritdoc />
    public async Task CreateAsync(Guid userId, string message, Guid? ticketId = null, string? type = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false,
            TicketId = ticketId,
            Type = type
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task NotifyDepartmentUsersAsync(Guid departmentId, string message, Guid? ticketId = null, string? type = null)
    {
        // Fetch all UnitUsers whose DepartmentId matches
        var users = await _userManager.Users
            .Where(u => u.DepartmentId == departmentId && !u.IsDeleted && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (!users.Any()) return;

        var notifications = users.Select(userId => new Notification
        {
            UserId = userId,
            Message = message,
            IsRead = false,
            TicketId = ticketId,
            Type = type
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();
    }
}
