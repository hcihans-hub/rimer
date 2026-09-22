namespace RimerApi.Application.DTOs.Auth;

/// <summary>
/// DTO for requesting a new access token using a refresh token.
/// </summary>
public class RefreshTokenDto
{
    /// <summary>The expired (or still valid) access token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The refresh token received during login/register.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}
