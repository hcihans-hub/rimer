using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace RimerApi.API.Providers;

/// <summary>
/// Custom provider to map our JWT NameIdentifier to the SignalR User identifier.
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst("sub")?.Value;
    }
}
