namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Request body for replying to a ticket.
/// </summary>
public class ReplyTicketDto
{
    /// <summary>Reply message content.</summary>
    public string Message { get; set; } = string.Empty;
}
