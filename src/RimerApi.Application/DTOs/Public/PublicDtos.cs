using System.ComponentModel.DataAnnotations;

namespace RimerApi.Application.DTOs.Public;

/// <summary>
/// Request body for creating a public (external) ticket without authentication.
/// </summary>
public class PublicApplyDto
{
    [MaxLength(11)]
    public string? IdentityNumber { get; set; }

    [MaxLength(100)]
    public string? TitleName { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public bool HidePersonalInfo { get; set; }

    [Required]
    public int Category { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Target department ID.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Optional attachment URL.</summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>Must be true to submit.</summary>
    [Required]
    public bool TermsAccepted { get; set; }
}

/// <summary>
/// Response after a public ticket is created.
/// </summary>
public class PublicApplyResponseDto
{
    public string TrackingCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request body for tracking a public ticket.
/// </summary>
public class PublicTrackRequestDto
{
    [Required]
    public string TrackingCode { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response for public ticket tracking.
/// </summary>
public class PublicTrackResponseDto
{
    public string TrackingCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? LatestReply { get; set; }
    public string? GuestFirstName { get; set; }
    public string? GuestLastName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestAddress { get; set; }
    public string? GuestIdentityNumber { get; set; }
    public string? GuestTitle { get; set; }
    public bool HidePersonalInfo { get; set; }
    public List<string> PastDepartments { get; set; } = new();
    public List<PublicTimelineItemDto> Timeline { get; set; } = new();
}

/// <summary>
/// A single event in the ticket's public timeline.
/// </summary>
public class PublicTimelineItemDto
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Detail { get; set; }
    public string? AuthorName { get; set; }
}

/// <summary>
/// Lightweight department info for the public form dropdown.
/// </summary>
public class PublicDepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
