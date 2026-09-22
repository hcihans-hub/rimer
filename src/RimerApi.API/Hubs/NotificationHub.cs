using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RimerApi.API.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications.
/// Handles connection lifecycles and group assignments.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        
        // Add Staff & Admins to a special group automatically upon connection
        if (role == "Admin" || role == "Staff")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "StaffAdmin");
        }
        
        await base.OnConnectedAsync();
    }
}
