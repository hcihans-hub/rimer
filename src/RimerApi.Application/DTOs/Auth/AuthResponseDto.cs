namespace RimerApi.Application.DTOs.Auth;

/// <summary>
/// DTO returned after successful login, registration, or token refresh.
/// </summary>
public class AuthResponseDto
{
    /// <summary>JWT access token (short-lived, 1 hour).</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Access token expiration time (UTC).</summary>
    public DateTime Expiration { get; set; }

    /// <summary>Refresh token (long-lived, 7 days). Use to get a new access token.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Refresh token expiration time (UTC).</summary>
    public DateTime RefreshTokenExpiration { get; set; }

    /// <summary>User's unique identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>User's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's last name.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>User's full name (computed).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>User's email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>User's role.</summary>
    public string Role { get; set; } = string.Empty;
}
