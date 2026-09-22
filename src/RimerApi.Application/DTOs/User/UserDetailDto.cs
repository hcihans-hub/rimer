namespace RimerApi.Application.DTOs.User;

/// <summary>
/// Detailed user info returned by admin endpoints.
/// Includes department, role, status, and audit timestamps.
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? PersonType { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
