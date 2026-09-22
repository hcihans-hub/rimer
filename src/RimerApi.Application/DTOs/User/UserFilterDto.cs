using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.User;

/// <summary>
/// Query parameters for filtering and paginating the user list.
/// </summary>
public class UserFilterDto
{
    /// <summary>Filter by role. Null = all roles.</summary>
    public UserRole? Role { get; set; }

    /// <summary>Filter by department id. Null = all departments.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Search by full name or email (case-insensitive, partial match).</summary>
    public string? Search { get; set; }

    /// <summary>Page number (1-based). Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page. Defaults to 20, max 100.</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Filter by active status. Default true.</summary>
    public bool OnlyActive { get; set; } = true;
}
