namespace RimerApi.Application.Common;

/// <summary>
/// Represents the currently authenticated user's identity and role.
/// Passed from controllers into services so authorization logic stays in the service layer.
/// Controllers extract this from JWT claims; services use it for access control decisions.
/// </summary>
public record CurrentUser
{
    /// <summary>User's unique identifier (from ClaimTypes.NameIdentifier).</summary>
    public Guid Id { get; init; }

    /// <summary>User's role name (from ClaimTypes.Role).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Department the user belongs to (null for Admin / Student).
    /// Embedded in JWT as "departmentId" claim.
    /// </summary>
    public Guid? DepartmentId { get; init; }

    /// <summary>Whether the user has the Admin role.</summary>
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user has Staff or Admin role.</summary>
    public bool IsStaffOrAdmin => IsAdmin || Role.Equals("Staff", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user is a UnitUser (department-scoped ticket handler).</summary>
    public bool IsUnitUser => Role.Equals("UnitUser", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user has the Rector role (read-only observer).</summary>
    public bool IsRector => Role.Equals("Rector", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user has the Auditor role (future structure, read-only observer of everything).</summary>
    public bool IsAuditor => Role.Equals("Auditor", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the user has the Operator role (can view all tickets, route to departments).</summary>
    public bool IsOperator => Role.Equals("Operator", StringComparison.OrdinalIgnoreCase);
}
