using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Auth;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Abstraction for authentication operations.
/// Implemented in Infrastructure using ASP.NET Identity + JWT.
/// </summary>
public interface IAuthService
{
    /// <summary>Register a new user.</summary>
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto);

    /// <summary>Authenticate a user and return JWT + refresh token.</summary>
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto);

    /// <summary>Refresh an expired access token using a valid refresh token.</summary>
    Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto);

    /// <summary>Logout — revoke the user's refresh token.</summary>
    Task<ServiceResult<bool>> LogoutAsync(Guid userId);

    /// <summary>Initiate password reset process.</summary>
    Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordDto dto);

    /// <summary>Complete password reset using token.</summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto dto);
}
