using System;
using System.Threading.Tasks;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Abstraction for real-time notification hubs. Keeps Application layer decoupled from SignalR.
/// </summary>
public interface INotificationClientService
{
    Task NotifyStaffAdminAsync(string title, string message);
    Task NotifyUserAsync(Guid userId, string title, string message);
}
