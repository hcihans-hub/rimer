using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.User;

/// <summary>
/// Lightweight user info DTO returned by IUserService.
/// Contains only the fields that other services need — no sensitive data.
/// </summary>
public class UserDto
{
    /// <summary>User's unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>First name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Full display name (computed).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Role name (Student, Staff, Admin).</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Category of the person (Student, Academic, etc.).</summary>
    public PersonType PersonType { get; set; }
}
