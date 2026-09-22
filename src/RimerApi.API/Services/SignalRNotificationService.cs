using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RimerApi.API.Hubs;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Services;

/// <summary>
/// Implements INotificationClientService using SignalR from the API layer.
/// This prevents Infrastructure and Application layers from coupling with Microsoft.AspNetCore.SignalR.
/// </summary>
public class SignalRNotificationService : INotificationClientService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyStaffAdminAsync(string title, string message)
    {
        return _hubContext.Clients.Group("StaffAdmin").SendAsync("ReceiveNotification", new 
        {
            Title = title, 
            Message = message, 
            Timestamp = DateTime.UtcNow 
        });
    }

    public Task NotifyUserAsync(Guid userId, string title, string message)
    {
        return _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new 
        { 
            Title = title, 
            Message = message, 
            Timestamp = DateTime.UtcNow 
        });
    }
}
