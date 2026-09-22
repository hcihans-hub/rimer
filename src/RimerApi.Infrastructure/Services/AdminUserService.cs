using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.User;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using RimerApi.Application.DTOs.Auth;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// IAdminUserService implementation using ASP.NET Identity's UserManager.
/// All user management operations that require Admin-level access live here.
/// </summary>
public class AdminUserService : IAdminUserService
{
    // The seeded admin is protected — cannot be deleted or demoted.
    private static readonly Guid SystemAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminUserService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ISystemLogService _systemLogService;
    private readonly IServiceProvider _serviceProvider;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<AdminUserService> logger,
        ISystemLogService systemLogService,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
        _systemLogService = systemLogService;
        _serviceProvider = serviceProvider;
    }

    // ── GetUsersAsync ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<PagedResult<UserDetailDto>>> GetUsersAsync(UserFilterDto filter)
    {
        // Clamp page size
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);
        filter.Page = Math.Max(filter.Page, 1);

        var query = _userManager.Users
            .Include(u => u.Department)
            .AsNoTracking();

        // ── Filters ─────────────────────────────────────────────────
        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(u => u.DepartmentId == filter.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search) ||
                (u.Email != null && u.Email.ToLower().Contains(search)));
        }

        if (filter.OnlyActive)
        {
            query = query.Where(u => u.IsActive);
        }

        // ── Pagination ───────────────────────────────────────────────
        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtos = users.Select(MapToDetailDto).ToList();

        return ServiceResult<PagedResult<UserDetailDto>>.Success(
            new PagedResult<UserDetailDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
    }

    // ── GetUserByIdAsync ────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<UserDetailDto>> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return ServiceResult<UserDetailDto>.NotFound($"User '{userId}' not found.");

        return ServiceResult<UserDetailDto>.Success(MapToDetailDto(user));
    }

    // ── CreateUserAsync ─────────────────────────────────────────────
    public async Task<ServiceResult<UserDetailDto>> CreateUserAsync(CreateUserDto dto, Guid callerId)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            return ServiceResult<UserDetailDto>.Failure("Bu e-posta adresi zaten kullanımda.", 409);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            UserName = email,
            Role = dto.Role,
            DepartmentId = dto.DepartmentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return ServiceResult<UserDetailDto>.Failure(errors, 400);
        }

        // Assign role in Identity
        var roleResult = await _userManager.AddToRoleAsync(user, dto.Role.ToString());
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Admin created user {UserId} but role assignment {Role} failed: {Errors}", 
                user.Id, dto.Role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        await _systemLogService.LogAsync(
            callerId.ToString(),
            "User Created",
            "User",
            user.Id.ToString(),
            $"Yeni kullan\u0131c\u0131 olu\u015fturuldu: {user.Email} (Rol: {user.Role}, Birim: {user.DepartmentId})"
        );

        return ServiceResult<UserDetailDto>.Success(MapToDetailDto(user));
    }

    // ── UpdateUserRoleAsync ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<UserDetailDto>> UpdateUserRoleAsync(
        Guid userId, UpdateUserRoleDto dto, Guid callerId)
    {
        if (userId == callerId)
            return ServiceResult<UserDetailDto>.Failure("You cannot change your own role.", 400);

        var user = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return ServiceResult<UserDetailDto>.NotFound($"User '{userId}' not found.");

        if (userId == SystemAdminId)
            return ServiceResult<UserDetailDto>.Failure("The system administrator role cannot be changed.", 403);

        var oldRole = user.Role.ToString();
        var newRole = dto.Role.ToString();

        if (oldRole != newRole)
        {
            // Update Identity role claim
            var removeResult = await _userManager.RemoveFromRoleAsync(user, oldRole);
            // We don't necessarily fail if remove fails (maybe they weren't in the role in Identity table)
            
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(" ", addResult.Errors.Select(e => e.Description));
                return ServiceResult<UserDetailDto>.Failure($"Rol atamas\u0131 ba\u015far\u0131s\u0131z oldu: {errors}", 400);
            }
        }

        // Update enum field on user (denormalized for query performance)
        user.Role = dto.Role;
        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
             var errors = string.Join(" ", updateResult.Errors.Select(e => e.Description));
             return ServiceResult<UserDetailDto>.Failure($"Kullan\u0131c\u0131 g\u00fcncellenemedi: {errors}", 500);
        }

        _logger.LogInformation(
            "Admin {CallerId} changed role of user {UserId} from {OldRole} to {NewRole}.",
            callerId, userId, oldRole, newRole);

        await _systemLogService.LogAsync(
            callerId.ToString(),
            "Role Changed",
            "User",
            userId.ToString(),
            $"Kullan\u0131c\u0131 rol\u00fc de\u011fi\u015ftirildi: {oldRole} \u2192 {newRole} ({user.Email})"
        );

        return ServiceResult<UserDetailDto>.Success(MapToDetailDto(user));
    }

    // ── UpdateUserAsync ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<UserDetailDto>> UpdateUserAsync(
        Guid userId, UpdateUserDto dto, Guid callerId)
    {
        var user = await _userManager.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return ServiceResult<UserDetailDto>.NotFound($"User '{userId}' not found.");

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.FirstName) && dto.FirstName != user.FirstName)
        {
            changes.Add($"FirstName: {user.FirstName} → {dto.FirstName}");
            user.FirstName = dto.FirstName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.LastName) && dto.LastName != user.LastName)
        {
            changes.Add($"LastName: {user.LastName} → {dto.LastName}");
            user.LastName = dto.LastName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            !dto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            // Check email uniqueness
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null && existing.Id != userId)
                return ServiceResult<UserDetailDto>.Failure("Email is already in use by another user.", 409);

            changes.Add($"Email: {user.Email} \u2192 {dto.Email}");
            user.Email = dto.Email.Trim().ToLowerInvariant();
            user.UserName = user.Email;
            user.NormalizedEmail = dto.Email.Trim().ToUpperInvariant();
            user.NormalizedUserName = user.NormalizedEmail;
        }

        // Department assignment
        var oldDeptId = user.DepartmentId;
        if (dto.DepartmentId != user.DepartmentId)
        {
            changes.Add($"DepartmentId: {user.DepartmentId} \u2192 {dto.DepartmentId}");
            user.DepartmentId = dto.DepartmentId;
        }

        if (!changes.Any())
            return ServiceResult<UserDetailDto>.Success(MapToDetailDto(user));

        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ServiceResult<UserDetailDto>.Failure(errors, 500);
        }

        // Log department change specifically
        if (oldDeptId != user.DepartmentId)
        {
            await _systemLogService.LogAsync(
                callerId.ToString(),
                "Department Changed",
                "User",
                userId.ToString(),
                $"Kullan\u0131c\u0131 birimi de\u011fi\u015ftirildi: {oldDeptId} \u2192 {user.DepartmentId} ({user.Email})"
            );
        }

        await _systemLogService.LogAsync(
            callerId.ToString(),
            "User Updated",
            "User",
            userId.ToString(),
            $"Kullan\u0131c\u0131 g\u00fcncellendi: {string.Join("; ", changes)}"
        );

        _logger.LogInformation(
            "Admin {CallerId} updated user {UserId}: {Changes}.",
            callerId, userId, string.Join("; ", changes));

        // Reload with navigation
        var updated = await _userManager.Users
            .Include(u => u.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        return ServiceResult<UserDetailDto>.Success(MapToDetailDto(updated!));
    }

    // ── DeleteUserAsync ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteUserAsync(Guid userId, Guid callerId)
    {
        if (userId == callerId)
            return ServiceResult<bool>.Failure("You cannot delete your own account.", 400);

        if (userId == SystemAdminId)
            return ServiceResult<bool>.Failure("The system administrator account cannot be deleted.", 403);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete user {UserId}: {Errors}", userId, errors);
            return ServiceResult<bool>.Failure(errors, 500);
        }

        _logger.LogInformation("Admin {CallerId} deleted user {UserId} ({Email}).",
            callerId, userId, user.Email);

        return ServiceResult<bool>.Success(true);
    }

    // ── LockUserAsync ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> LockUserAsync(Guid userId, Guid callerId)
    {
        if (userId == callerId)
            return ServiceResult<bool>.Failure("You cannot lock your own account.", 400);

        if (userId == SystemAdminId)
            return ServiceResult<bool>.Failure("The system administrator account cannot be locked.", 403);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        // Lock for 100 years — effectively a permanent ban until manually unlocked
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        await _userManager.SetLockoutEnabledAsync(user, true);

        _logger.LogWarning("Admin {CallerId} locked user {UserId} ({Email}).",
            callerId, userId, user.Email);

        return ServiceResult<bool>.Success(true);
    }

    // ── UnlockUserAsync ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> UnlockUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("User {UserId} ({Email}) has been unlocked.", userId, user.Email);

        return ServiceResult<bool>.Success(true);
    }

    // ── ActivateUserAsync ───────────────────────────────────────────
    public async Task<ServiceResult<bool>> ActivateUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        return ServiceResult<bool>.Success(true);
    }

    // ── DeactivateUserAsync ─────────────────────────────────────────
    public async Task<ServiceResult<bool>> DeactivateUserAsync(Guid userId, Guid callerId)
    {
        if (userId == callerId)
            return ServiceResult<bool>.Failure("You cannot deactivate your own account.", 400);

        if (userId == SystemAdminId)
            return ServiceResult<bool>.Failure("The system administrator account cannot be deactivated.", 403);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        return ServiceResult<bool>.Success(true);
    }

    // ── Send Password Reset Link ────────────────────────────────────
    public async Task<ServiceResult<bool>> SendPasswordResetLinkAsync(Guid userId, Guid callerId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ServiceResult<bool>.NotFound($"User '{userId}' not found.");

        if (string.IsNullOrEmpty(user.Email))
            return ServiceResult<bool>.Failure("Kullanıcının geçerli bir e-posta adresi yok.");

        using var scope = _serviceProvider.CreateScope();
        var authService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IAuthService>(scope.ServiceProvider);

        var result = await authService.ForgotPasswordAsync(new ForgotPasswordDto { Email = user.Email });
        if (!result.IsSuccess)
        {
            return ServiceResult<bool>.Failure("Şifre sıfırlama bağlantısı gönderilemedi: " + result.ErrorMessage);
        }

        await _systemLogService.LogAsync(
            callerId.ToString(),
            "Password Reset Link Sent",
            "User",
            userId.ToString(),
            $"Admin tarafından şifre sıfırlama bağlantısı gönderildi: {user.Email}"
        );

        return ServiceResult<bool>.Success(true);
    }

    // ── Private Mapping ─────────────────────────────────────────────

    private static UserDetailDto MapToDetailDto(ApplicationUser user)
    {
        return new UserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            PersonType = user.PersonType.ToString(),
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive,
            CreatedBy = user.CreatedBy,
            LastLoginAt = user.LastLoginAt
        };
    }
}
