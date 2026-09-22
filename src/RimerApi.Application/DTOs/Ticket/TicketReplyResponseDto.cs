namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Response DTO for a TicketReply.
/// </summary>
public class TicketReplyResponseDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
