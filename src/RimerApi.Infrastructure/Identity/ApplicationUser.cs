using Microsoft.AspNetCore.Identity;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Infrastructure.Identity;

/// <summary>
/// The single source of truth for users in the system.
/// Extends IdentityUser&lt;Guid&gt; with application-specific properties.
///
/// Domain entities (Ticket, TicketHistory) reference users by Guid only.
/// All user operations go through UserManager&lt;ApplicationUser&gt;.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, ISoftDeletable
{
    /// <summary>First name of the user.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name of the user.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Full name (computed from FirstName + LastName). Kept for backward compatibility.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Role determining the user's permissions.</summary>
    public UserRole Role { get; set; }

    /// <summary>Optional department the user belongs to.</summary>
    public Guid? DepartmentId { get; set; }

    // ── Identity Sync Fields ────────────────────────────────────────

    /// <summary>External identity system ID (e.g. LDAP uid, e-Devlet TC, SSO sub).</summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Person type synced from external identity service.
    /// Used for analytics (not for authorization — use <see cref="Role"/> for that).
    /// </summary>
    public PersonType PersonType { get; set; } = PersonType.Other;

    /// <summary>UTC timestamp when identity data was last synced from external service.</summary>
    public DateTime? LastSyncedAt { get; set; }

    // ── Timestamps ──────────────────────────────────────────────────

    /// <summary>UTC timestamp when the user was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Who created the user.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Whether the user is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when the user was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Current refresh token (null if logged out).</summary>
    public string? RefreshToken { get; set; }

    /// <summary>When the refresh token expires (UTC).</summary>
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>UTC timestamp of the user's last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    // ── Navigation Properties ──────────────────────────────────────

    /// <summary>Department the user belongs to.</summary>
    public Department? Department { get; set; }

    // ── ISoftDeletable ─────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
