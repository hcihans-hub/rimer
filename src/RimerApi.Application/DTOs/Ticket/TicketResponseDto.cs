using RimerApi.Application.DTOs.User;

namespace RimerApi.Application.DTOs.Ticket;

/// <summary>
/// Full response DTO for a ticket, including resolved user names and history.
/// </summary>
public class TicketResponseDto
{
    /// <summary>Ticket id.</summary>
    public Guid Id { get; set; }

    /// <summary>Ticket title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Ticket description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Human-readable reference number.</summary>
    public string ReferenceNo { get; set; } = string.Empty;

    /// <summary>Category name (e.g. "Complaint", "Suggestion").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Status name (e.g. "Submitted", "Reviewing", "Closed").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Internal Status (e.g. "Unread", "Read").</summary>
    public string InternalStatus { get; set; } = string.Empty;

    /// <summary>Priority level (1=Low, 2=Normal, 3=Important, 4=Critical).</summary>
    public int Priority { get; set; }

    /// <summary>Is it an external ticket?</summary>
    public bool IsExternal { get; set; }

    /// <summary>Whether the applicant requested their personal info to be hidden.</summary>
    public bool HidePersonalInfo { get; set; }

    /// <summary>First Name of the external applicant.</summary>
    public string? GuestName { get; set; }

    /// <summary>Surname of the external applicant.</summary>
    public string? GuestSurname { get; set; }

    /// <summary>Email of the external applicant.</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Phone number of the external applicant.</summary>
    public string? GuestPhone { get; set; }

    /// <summary>TC Identity Number of the external applicant.</summary>
    public string? GuestIdentityNumber { get; set; }

    /// <summary>Title/Profession of the external applicant.</summary>
    public string? GuestTitle { get; set; }

    /// <summary>Address of the external applicant.</summary>
    public string? GuestAddress { get; set; }

    /// <summary>Institution the ticket is addressed to.</summary>
    public string? InstitutionName { get; set; }

    /// <summary>Attachment URL, if any.</summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>Creator user info (resolved via IUserService).</summary>
    public UserDto? Creator { get; set; }

    /// <summary>Assigned staff user info (resolved via IUserService).</summary>
    public UserDto? AssignedTo { get; set; }

    /// <summary>Department name the ticket was initially routed to.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Department id (initial routing).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Department currently responsible for the ticket (authoritative for access control).
    /// </summary>
    public Guid? AssignedDepartmentId { get; set; }

    /// <summary>Name of the department currently responsible for the ticket.</summary>
    public string? AssignedDepartmentName { get; set; }

    /// <summary>UTC timestamp of the last transfer (used for priority sorting).</summary>
    public DateTime? LastTransferredAt { get; set; }

    /// <summary>When the ticket was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the ticket was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Chronological history of changes (only included in detail view).</summary>
    public List<TicketHistoryResponseDto>? Histories { get; set; }

    /// <summary>Transfer history (only included in detail view).</summary>
    public List<TicketTransferResponseDto>? Transfers { get; set; }

    /// <summary>Staff replies on the ticket.</summary>
    public List<TicketReplyResponseDto>? Replies { get; set; }
}

