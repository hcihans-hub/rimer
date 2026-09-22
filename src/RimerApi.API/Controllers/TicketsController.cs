using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Extensions;
using RimerApi.API.Models;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IWebHostEnvironment _environment;

    public TicketsController(ITicketService ticketService, IWebHostEnvironment environment)
    {
        _ticketService = ticketService;
        _environment = environment;
    }

    /// <summary>
    /// Create a new ticket. Any authenticated user can create.
    /// </summary>
    /// <response code="201">Ticket created successfully.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromForm] CreateTicketDto dto, IFormFile? attachment)
    {
        // 1. Mandatory Terms Check
        if (!dto.TermsAccepted)
            return BadRequest(ApiResponse<object>.Fail("Terms must be accepted.", 400));

        // 2. File Validation & Upload
        string? attachmentUrl = null;
        if (attachment != null)
        {
            // Size check (1MB)
            if (attachment.Length > 1 * 1024 * 1024)
                return BadRequest(ApiResponse<object>.Fail("File size cannot exceed 1MB.", 400));

            // Extension check
            var allowedExtensions = new[] { ".docx", ".jpeg", ".jpg", ".png" };
            var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(ApiResponse<object>.Fail("Only .docx, .jpeg, .jpg, .png files are allowed.", 400));

            // Save to disk (wwwroot/uploads)
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await attachment.CopyToAsync(stream);
            }

            attachmentUrl = $"/uploads/{fileName}";
        }

        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.CreateAsync(dto, attachmentUrl, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return StatusCode(201,
            ApiResponse<TicketResponseDto>.Created(result.Data!, "Ticket created successfully."));
    }

    /// <summary>
    /// Get a filtered, paginated list of tickets.
    /// Students only see their own tickets. Staff/Admin see all.
    /// </summary>
    /// <response code="200">Tickets retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TicketResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] TicketFilterDto filter)
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.GetListAsync(filter, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<PagedResult<TicketResponseDto>>.Ok(result.Data!));
    }

    /// <summary>
    /// Get dashboard stats for the current user (e.g. counts for their department).
    /// </summary>
    [HttpGet("my-stats")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyStats()
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.GetMyStatsAsync(currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<DepartmentStatsDto>.Ok(result.Data!));
    }

    /// <summary>
    /// Get a ticket by id with full details and history.
    /// Students can only view their own tickets. Staff/Admin can view all.
    /// </summary>
    /// <param name="id">Ticket id.</param>
    /// <response code="200">Ticket retrieved successfully.</response>
    /// <response code="403">You do not have permission to view this ticket.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.GetByIdAsync(id, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 404,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 404));

        return Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!));
    }

    /// <summary>
    /// Update the status of a ticket. Staff and Admin only.
    /// </summary>
    /// <param name="id">Ticket id.</param>
    /// <param name="dto">New status data.</param>
    /// <response code="200">Status updated successfully.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTicketStatusDto dto)
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.UpdateStatusAsync(id, dto, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!, "Ticket status updated."));
    }

    /// <summary>
    /// Assign a ticket to a staff member and/or department. Admin only.
    /// </summary>
    /// <param name="id">Ticket id.</param>
    /// <param name="dto">Assignment data.</param>
    /// <response code="200">Ticket assigned successfully.</response>
    /// <response code="403">Only Admin can assign tickets.</response>
    /// <response code="404">Ticket, user, or department not found.</response>
    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTicketDto dto)
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.AssignAsync(id, dto, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!, "Ticket assigned successfully."));
    }

    /// <summary>
    /// Delete a ticket (soft delete). Admin only.
    /// </summary>
    /// <param name="id">Ticket id.</param>
    /// <response code="200">Ticket deleted successfully.</response>
    /// <response code="403">Only Admin can delete tickets.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var currentUser = User.GetCurrentUser();
        var result = await _ticketService.DeleteAsync(id, currentUser);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<bool>.Ok(true, "Ticket deleted successfully."));
    }
    
    /// <summary>
    /// Transfer a ticket to a different department.
    /// Requires UnitUser (in current dept) or Admin role.
    /// </summary>
    [HttpPost("{id}/transfer")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferTicketDto dto)
    {
        var result = await _ticketService.TransferAsync(id, dto, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }

    /// <summary>
    /// Reply to a ticket.
    /// Requires UnitUser (in current dept), Staff, or Admin role.
    /// </summary>
    [HttpPost("{id}/reply")]
    [Authorize(Roles = "UnitUser,Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<TicketReplyResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyTicketDto dto)
    {
        var result = await _ticketService.ReplyAsync(id, dto, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketReplyResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }

    /// <summary>
    /// Close a ticket.
    /// Requires UnitUser (in current dept), Staff, or Admin role.
    /// </summary>
    [HttpPost("{id}/close")]
    [Authorize(Roles = "UnitUser,Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<TicketResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await _ticketService.CloseAsync(id, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }

    [HttpPost("{id}/take-ownership")]
    [Authorize]
    public async Task<IActionResult> TakeOwnership(Guid id)
    {
        var result = await _ticketService.TakeOwnershipAsync(id, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }

    [HttpPut("{id}/internal-status")]
    [Authorize]
    public async Task<IActionResult> UpdateInternalStatus(Guid id, [FromBody] int internalStatus)
    {
        var result = await _ticketService.UpdateInternalStatusAsync(id, internalStatus, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }

    [HttpPut("{id}/priority")]
    [Authorize]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromBody] int priority)
    {
        var result = await _ticketService.UpdatePriorityAsync(id, priority, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<TicketResponseDto>.Ok(result.Data!)) : HandleFailure(result);
    }


    [HttpGet("reminders")]
    [Authorize]
    public async Task<IActionResult> GetReminders()
    {
        var result = await _ticketService.GetActiveRemindersAsync(User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<List<TicketReminderDto>>.Ok(result.Data!)) : HandleFailure(result);
    }

    /// <summary>
    /// Dismiss a reminder.
    /// </summary>
    [HttpPut("reminders/{reminderId}/dismiss")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DismissReminder(int reminderId)
    {
        var result = await _ticketService.DismissReminderAsync(reminderId, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<bool>.Ok(result.Data!)) : HandleFailure(result);
    }

    /// <summary>
    /// Create a reminder for a ticket.
    /// </summary>
    [HttpPost("{id}/reminders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateReminder(Guid id, [FromBody] CreateTicketReminderDto dto)
    {
        var result = await _ticketService.CreateReminderAsync(id, dto, User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<bool>.Ok(result.Data!)) : HandleFailure(result);
    }

    /// <summary>
    /// Snooze a reminder.
    /// </summary>
    [HttpPut("reminders/{reminderId}/snooze")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SnoozeReminder(int reminderId, [FromBody] DateTime snoozeUntil)
    {
        var result = await _ticketService.SnoozeReminderAsync(reminderId, snoozeUntil.ToUniversalTime(), User.GetCurrentUser());
        return result.IsSuccess ? Ok(ApiResponse<bool>.Ok(result.Data!)) : HandleFailure(result);
    }

    // ── Helper ──────────────────────────────────────────────────

    private ObjectResult HandleFailure<T>(ServiceResult<T> result)
    {
        return StatusCode(result.ErrorCode ?? 400,
            ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));
    }
}
