namespace RimerApi.Application.DTOs.Auth;

/// <summary>
/// DTO for user login request.
/// </summary>
public class LoginDto
{
    /// <summary>User's email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>User's password.</summary>
    public string Password { get; set; } = string.Empty;
}
