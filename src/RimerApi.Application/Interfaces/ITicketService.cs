using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Application.DTOs.Analytics;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Service interface for ticket business operations.
/// All methods receive <see cref="CurrentUser"/> for authorization checks inside the service layer.
/// </summary>
public interface ITicketService
{
    /// <summary>Create a new ticket. Any authenticated user can create.</summary>
    Task<ServiceResult<TicketResponseDto>> CreateAsync(CreateTicketDto dto, string? attachmentUrl, CurrentUser currentUser);

    /// <summary>
    /// Get a ticket by id with full details and history.
    /// Students can only view their own tickets. UnitUsers only within their dept. Admin all.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> GetByIdAsync(Guid ticketId, CurrentUser currentUser);

    /// <summary>
    /// Get a filtered, paginated list of tickets.
    /// Students → own tickets only.
    /// UnitUsers → AssignedDepartmentId == their dept only.
    /// Staff/Admin → all tickets.
    /// </summary>
    Task<ServiceResult<PagedResult<TicketResponseDto>>> GetListAsync(TicketFilterDto filter, CurrentUser currentUser);

    /// <summary>
    /// Update the status of a ticket.
    /// Only Staff and Admin can update status.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> UpdateStatusAsync(Guid ticketId, UpdateTicketStatusDto dto, CurrentUser currentUser);

    /// <summary>
    /// Assign a ticket to a staff member and/or department.
    /// Only Admin can assign tickets.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> AssignAsync(Guid ticketId, AssignTicketDto dto, CurrentUser currentUser);

    /// <summary>
    /// Transfer a ticket to a different department.
    /// UnitUser (must be in current dept) or Admin can transfer.
    /// Old department loses visibility; new department gains it.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> TransferAsync(Guid ticketId, TransferTicketDto dto, CurrentUser currentUser);

    /// <summary>
    /// Add a staff / unit-user reply to a ticket.
    /// Notifies the ticket owner.
    /// </summary>
    Task<ServiceResult<TicketReplyResponseDto>> ReplyAsync(Guid ticketId, ReplyTicketDto dto, CurrentUser currentUser);

    /// <summary>
    /// Close a ticket and notify the ticket owner.
    /// UnitUser (must be in assigned dept) or Admin/Staff can close.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> CloseAsync(Guid ticketId, CurrentUser currentUser);

    /// <summary>
    /// Take ownership of a ticket (assigns to the calling user).
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> TakeOwnershipAsync(Guid ticketId, CurrentUser currentUser);

    /// <summary>
    /// Update internal department status.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> UpdateInternalStatusAsync(Guid ticketId, int internalStatus, CurrentUser currentUser);

    /// <summary>
    /// Update priority of a ticket.
    /// </summary>
    Task<ServiceResult<TicketResponseDto>> UpdatePriorityAsync(Guid ticketId, int priority, CurrentUser currentUser);

    /// <summary>
    /// Create a reminder for a ticket.
    /// </summary>
    Task<ServiceResult<bool>> CreateReminderAsync(Guid ticketId, CreateTicketReminderDto dto, CurrentUser currentUser);

    /// <summary>
    /// Dismiss a reminder.
    /// </summary>
    Task<ServiceResult<bool>> DismissReminderAsync(int reminderId, CurrentUser currentUser);

    /// <summary>
    /// Snooze a reminder.
    /// </summary>
    Task<ServiceResult<bool>> SnoozeReminderAsync(int reminderId, DateTime snoozeUntil, CurrentUser currentUser);

    /// <summary>
    /// Get active reminders for the current user.
    /// </summary>
    Task<ServiceResult<List<TicketReminderDto>>> GetActiveRemindersAsync(CurrentUser currentUser);

    /// <summary>
    /// Get stats for the current user's department/own view.
    /// </summary>
    Task<ServiceResult<DepartmentStatsDto>> GetMyStatsAsync(CurrentUser currentUser);

    /// <summary>
    /// Get analytics data for charts.
    /// Only accessible by chartsrole.
    /// </summary>
    Task<ServiceResult<ChartsResponseDto>> GetChartsAsync(DateTime? startDate, DateTime? endDate, CurrentUser currentUser);

    /// <summary>
    /// Delete a ticket (soft delete). Admin only.
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid ticketId, CurrentUser currentUser);
}

