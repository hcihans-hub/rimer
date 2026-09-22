using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Extensions;
using RimerApi.API.Models;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.User;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing user accounts.
/// All routes require the Admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly ISystemLogService _systemLogService;

    public UsersController(IAdminUserService adminUserService, ISystemLogService systemLogService)
    {
        _adminUserService = adminUserService;
        _systemLogService = systemLogService;
    }

    /// <summary>
    /// Get a paginated, filtered list of all users.
    /// </summary>
    /// <response code="200">User list retrieved.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDetailDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
    {
        var result = await _adminUserService.GetUsersAsync(filter);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<PagedResult<UserDetailDto>>.Ok(result.Data!));
    }

    /// <summary>
    /// Create a new user with specific role and department.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.CreateUserAsync(dto, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return StatusCode(201, ApiResponse<UserDetailDto>.Created(result.Data!, "User created successfully."));
    }

    /// <summary>
    /// Get full details of a single user by their id.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <response code="200">User found.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _adminUserService.GetUserByIdAsync(id);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 404,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 404));

        return Ok(ApiResponse<UserDetailDto>.Ok(result.Data!));
    }

    /// <summary>
    /// Update a user's profile (FullName, Email, DepartmentId). Admin only.
    /// Old ticket assignments remain unchanged when department changes.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="dto">Updated user data.</param>
    /// <response code="200">User updated.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.UpdateUserAsync(id, dto, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<UserDetailDto>.Ok(result.Data!, "User updated successfully."));
    }

    /// <summary>
    /// Change a user's role. Admins cannot change their own role.
    /// The system admin account is protected.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="dto">New role data.</param>
    /// <response code="200">Role updated.</response>
    /// <response code="400">Invalid request (e.g., self-demotion).</response>
    /// <response code="403">Cannot modify the system admin.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateUserRoleDto dto)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.UpdateUserRoleAsync(id, dto, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<UserDetailDto>.Ok(result.Data!, "User role updated successfully."));
    }

    /// <summary>
    /// Permanently delete a user account.
    /// Admins cannot delete themselves or the system admin.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <response code="200">User deleted.</response>
    /// <response code="400">Cannot delete own account.</response>
    /// <response code="403">Cannot delete the system admin.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.DeleteUserAsync(id, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<bool>.Ok(true, "User deleted successfully."));
    }

    /// <summary>
    /// Lock a user account (prevents login). Admin only.
    /// Duration: indefinite until manually unlocked.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <response code="200">User locked.</response>
    /// <response code="400">Cannot lock own account.</response>
    /// <response code="403">Cannot lock the system admin.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}/lock")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LockUser(Guid id)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.LockUserAsync(id, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<bool>.Ok(true, "User account locked."));
    }

    /// <summary>
    /// Unlock a previously locked user account.
    /// Also resets failed login attempt counter.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <response code="200">User unlocked.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var result = await _adminUserService.UnlockUserAsync(id);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 404,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 404));

        return Ok(ApiResponse<bool>.Ok(true, "User account unlocked."));
    }

    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var result = await _adminUserService.ActivateUserAsync(id);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 404,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 404));

        var userResult = await _adminUserService.GetUserByIdAsync(id);
        var email = userResult.IsSuccess ? userResult.Data!.Email : id.ToString();

        await _systemLogService.LogAsync(
            User.GetUserId().ToString(),
            "Activate User",
            "User",
            id.ToString(),
            $"User {email} was activated."
        );

        return Ok(ApiResponse<bool>.Ok(true, "User activated successfully."));
    }

    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.DeactivateUserAsync(id, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        var userResult = await _adminUserService.GetUserByIdAsync(id);
        var email = userResult.IsSuccess ? userResult.Data!.Email : id.ToString();

        await _systemLogService.LogAsync(
            callerId.ToString(),
            "Deactivate User",
            "User",
            id.ToString(),
            $"User {email} was deactivated."
        );

        return Ok(ApiResponse<bool>.Ok(true, "User deactivated successfully."));
    }

    [HttpPost("{id:guid}/send-reset-link")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendPasswordResetLink(Guid id)
    {
        var callerId = User.GetUserId();
        var result = await _adminUserService.SendPasswordResetLinkAsync(id, callerId);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<bool>.Ok(true, "Şifre sıfırlama bağlantısı kullanıcıya gönderildi."));
    }
}
