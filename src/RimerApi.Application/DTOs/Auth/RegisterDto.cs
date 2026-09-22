namespace RimerApi.Application.DTOs.Auth;

/// <summary>
/// DTO for user registration request.
/// Role is NOT accepted from client — all public registrations are Student.
/// Only an Admin can elevate a user's role via a privileged endpoint.
/// </summary>
public class RegisterDto
{
    /// <summary>First name of the user.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name of the user.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Email address (used as login identifier).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password (must meet complexity requirements).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Password confirmation.</summary>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Optional department id.</summary>
    public Guid? DepartmentId { get; set; }
}
