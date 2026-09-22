using RimerApi.Application.Common;
using RimerApi.Application.DTOs.User;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Admin-level user management operations.
/// Implemented in Infrastructure using UserManager&lt;ApplicationUser&gt;.
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Get a paginated, filtered list of all users.
    /// </summary>
    Task<ServiceResult<PagedResult<UserDetailDto>>> GetUsersAsync(UserFilterDto filter);

    /// <summary>
    /// Get full details of a single user by id.
    /// </summary>
    Task<ServiceResult<UserDetailDto>> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Update user profile (FullName, Email, DepartmentId).
    /// Logs the department-change action if DepartmentId changes.
    /// </summary>
    Task<ServiceResult<UserDetailDto>> UpdateUserAsync(Guid userId, UpdateUserDto dto, Guid callerId);

    /// <summary>
    /// Creates a new user with specific role and department (Admin only).
    /// </summary>
    Task<ServiceResult<UserDetailDto>> CreateUserAsync(CreateUserDto dto, Guid callerId);

    /// <summary>
    /// Change a user's role. Also updates the ASP.NET Identity role assignment.
    /// The caller (Admin) cannot demote themselves.
    /// </summary>
    Task<ServiceResult<UserDetailDto>> UpdateUserRoleAsync(Guid userId, UpdateUserRoleDto dto, Guid callerId);

    /// <summary>
    /// Permanently delete a user account. Admin only.
    /// The system admin account (seeded) cannot be deleted.
    /// </summary>
    Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid callerId);

    /// <summary>
    /// Lock a user account for the specified duration.
    /// </summary>
    Task<ServiceResult<bool>> LockUserAsync(Guid userId, Guid callerId);

    /// <summary>
    /// Unlock a previously locked user account.
    /// </summary>
    Task<ServiceResult<bool>> UnlockUserAsync(Guid userId);

    /// <summary>
    /// Activate a user.
    /// </summary>
    Task<ServiceResult<bool>> ActivateUserAsync(Guid userId);

    /// <summary>
    /// Deactivate a user.
    /// </summary>
    Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid callerId);

    /// <summary>
    /// Send a password reset link to a user manually via Admin.
    /// </summary>
    Task<ServiceResult<bool>> SendPasswordResetLinkAsync(Guid userId, Guid callerId);
}
