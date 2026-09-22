namespace RimerApi.Domain.Enums;

/// <summary>
/// Represents the role assigned to a user within the system.
/// </summary>
public enum UserRole
{
    /// <summary>A student / regular user with basic ticket creation privileges.</summary>
    Student = 0,

    /// <summary>A unit-level user who handles tickets inside their own department.</summary>
    UnitUser = 1,

    /// <summary>A staff member who can manage and be assigned tickets.</summary>
    Staff = 2,

    /// <summary>An administrator with full system access.</summary>
    Admin = 3,

    /// <summary>A high-level analytics user (Rectorate).</summary>
    Rector = 4,

    /// <summary>An operator who can view all tickets and route them to departments.</summary>
    Operator = 5
}
