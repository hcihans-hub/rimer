namespace RimerApi.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a ticket.
/// </summary>
public enum TicketStatus
{
    Submitted = 0,
    Reviewing = 1,
    WaitingDepartment = 2,
    Answered = 3,
    Closed = 4
}
