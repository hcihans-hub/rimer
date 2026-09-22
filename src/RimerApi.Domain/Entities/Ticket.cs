using RimerApi.Domain.Enums;

namespace RimerApi.Domain.Entities;

/// <summary>
/// Represents a communication ticket submitted to the rectorate.
/// Supports both internal (authenticated) and external (public) submissions.
/// <para>
/// AUTHORIZATION RULE: Only users whose DepartmentId matches
/// <see cref="AssignedDepartmentId"/> can see or act on this ticket.
/// </para>
/// </summary>
public class Ticket : BaseEntity
{
    /// <summary>Short title summarizing the ticket subject.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed description of the ticket.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Human-readable reference number (e.g. RIM-2026-0001).</summary>
    public string ReferenceNo { get; set; } = string.Empty;

    /// <summary>Category classifying the ticket type.</summary>
    public TicketCategory Category { get; set; }

    /// <summary>Current lifecycle status of the ticket.</summary>
    public TicketStatus Status { get; set; }

    /// <summary>Priority level.</summary>
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    /// <summary>Internal status for department workflow.</summary>
    public TicketInternalStatus InternalStatus { get; set; } = TicketInternalStatus.Unread;

    /// <summary>Optional attachment URL.</summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>Institution the ticket is addressed to.</summary>
    public string? InstitutionName { get; set; }

    /// <summary>Whether user accepted the terms.</summary>
    public bool TermsAccepted { get; set; }

    // ── Creator ─────────────────────────────────────────────────────

    /// <summary>
    /// Id of the user who created the ticket.
    /// Nullable for external/public tickets where no account exists.
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>Id of the staff member assigned to handle the ticket.</summary>
    public Guid? AssignedToId { get; set; }

    // ── External / Public Ticket Fields ─────────────────────────────

    /// <summary>Whether this ticket was submitted by an external (public) applicant.</summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// Unique tracking code for public tracking (e.g. RIM-2026-000001).
    /// Generated for all tickets, used by external applicants to track status.
    /// </summary>
    public string? TrackingCode { get; set; }

    // ── Security / Permissions ──────────────────────────────────────

    /// <summary>
    /// Indicates whether the ticket contains sensitive data.
    /// Used for masking architecture. Normal admins might see masked data,
    /// while auditors/super-admins see full content.
    /// </summary>
    public bool? ContainsSensitiveData { get; set; }

    /// <summary>TC Identity Number of the external applicant (optional).</summary>
    public string? GuestIdentityNumber { get; set; }

    /// <summary>Title/Profession of the external applicant (optional).</summary>
    public string? GuestTitle { get; set; }

    /// <summary>First Name of the external applicant (only for external tickets).</summary>
    public string? GuestName { get; set; }

    /// <summary>Surname of the external applicant (only for external tickets).</summary>
    public string? GuestSurname { get; set; }

    /// <summary>Address of the external applicant.</summary>
    public string? GuestAddress { get; set; }

    /// <summary>Email of the external applicant (only for external tickets).</summary>
    public string? GuestEmail { get; set; }

    /// <summary>Phone number of the external applicant (only for external tickets).</summary>
    public string? GuestPhone { get; set; }

    /// <summary>Whether the applicant requested their personal info to be hidden.</summary>
    public bool HidePersonalInfo { get; set; }

    // ── Department ──────────────────────────────────────────────────

    /// <summary>
    /// Id of the department the ticket was INITIALLY routed to (set on creation, informational).
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Id of the department CURRENTLY responsible for the ticket.
    /// This is the authoritative field for department-based access control.
    /// When a transfer occurs this field is updated; the old department loses visibility.
    /// </summary>
    public Guid? AssignedDepartmentId { get; set; }

    /// <summary>
    /// UTC timestamp of the last transfer.  Used for sorting priority tickets.
    /// </summary>
    public DateTime? LastTransferredAt { get; set; }

    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>Department the ticket was initially routed to.</summary>
    public Department? Department { get; set; }

    /// <summary>Department currently responsible for the ticket.</summary>
    public Department? AssignedDepartment { get; set; }

    /// <summary>Chronological history of changes made to this ticket.</summary>
    public ICollection<TicketHistory> Histories { get; set; } = new List<TicketHistory>();

    /// <summary>Transfer records for this ticket.</summary>
    public ICollection<TicketTransfer> Transfers { get; set; } = new List<TicketTransfer>();

    /// <summary>Staff replies on this ticket.</summary>
    public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
}
