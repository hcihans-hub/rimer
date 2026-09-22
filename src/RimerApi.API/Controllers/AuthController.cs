using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RimerApi.API.Extensions;
using RimerApi.API.Models;
using RimerApi.Application.DTOs.Auth;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISystemLogService _systemLogService;

    public AuthController(IAuthService authService, ISystemLogService systemLogService)
    {
        _authService = authService;
        _systemLogService = systemLogService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <response code="201">User registered successfully. Returns JWT + refresh token.</response>
    /// <response code="400">Validation error or duplicate email.</response>
    [HttpPost("register")]
    [EnableRateLimiting("RegisterPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        if (User.Identity?.IsAuthenticated == true)
        {
            var callerId = User.GetUserId().ToString();
            await _systemLogService.LogAsync(callerId, "Create User", "User", result.Data!.UserId.ToString());
        }

        return StatusCode(201, ApiResponse<AuthResponseDto>.Created(result.Data!,
            "User registered successfully."));
    }

    /// <summary>
    /// Authenticate and receive JWT + refresh token.
    /// </summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 401,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 401));

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data!, "Login successful."));
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 401,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 401));

        return Ok(ApiResponse<AuthResponseDto>.Ok(result.Data!, "Token refreshed successfully."));
    }

    /// <summary>
    /// Logout — revokes the current user's refresh token.
    /// </summary>
    /// <response code="200">Logged out successfully.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        var result = await _authService.LogoutAsync(userId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully. Refresh token revoked."));
    }

    /// <summary>
    /// Request a password reset link to be sent to email.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var result = await _authService.ForgotPasswordAsync(dto);
        if (!result.IsSuccess)
        {
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi."));
    }

    /// <summary>
    /// Reset password using token.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto);
        if (!result.IsSuccess)
        {
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz."));
    }
}
