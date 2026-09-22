using RimerApi.Application.DTOs.User;

namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Response DTO representing a single ticket history entry.
/// </summary>
public class TicketHistoryResponseDto
{
    /// <summary>History entry id.</summary>
    public Guid Id { get; set; }

    /// <summary>Description of the action performed.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Previous value before the change.</summary>
    public string? OldValue { get; set; }

    /// <summary>New value after the change.</summary>
    public string? NewValue { get; set; }

    /// <summary>User who made the change.</summary>
    public UserDto? ChangedBy { get; set; }

    /// <summary>When the change occurred.</summary>
    public DateTime ChangedAt { get; set; }
}
