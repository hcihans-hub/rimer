using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RimerApi.Application.DTOs.User;
using RimerApi.Application.Interfaces;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// IUserService implementation using ASP.NET Identity's UserManager.
/// This is the only place in the entire solution that UserManager is used for business queries.
///
/// Lives in Infrastructure because it depends on ApplicationUser and UserManager —
/// the Application layer only sees the IUserService abstraction.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByIdAsync(Guid userId, bool includeDeleted = false)
    {
        var query = _userManager.Users.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId);
        return user is null ? null : MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, UserDto>> GetByIdsAsync(IEnumerable<Guid> userIds, bool includeDeleted = false)
    {
        var idList = userIds.Distinct().ToList();

        if (!idList.Any())
            return new Dictionary<Guid, UserDto>();

        // Query ApplicationUser DbSet directly for batch efficiency
        var query = _userManager.Users.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();

        var users = await query
            .Where(u => idList.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();

        return users.ToDictionary(u => u.Id, MapToDto);
    }

    /// <inheritdoc />
    public async Task<List<Guid>> SearchUserIdsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<Guid>();

        return await _userManager.Users
            .Where(u => u.FirstName.Contains(searchTerm) || u.LastName.Contains(searchTerm) || (u.Email != null && u.Email.Contains(searchTerm)))
            .Select(u => u.Id)
            .ToListAsync();
    }

    // ── Private helpers ────────────────────────────────────────────

    private static UserDto MapToDto(ApplicationUser user)
    {
        if (user.IsDeleted)
        {
            return new UserDto
            {
                Id = user.Id,
                FirstName = "Deleted",
                LastName = "User",
                FullName = "Deleted User",
                Email = "deleted@user.local", // Masked
                Role = user.Role.ToString()
            };
        }

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            PersonType = user.PersonType
        };
    }
}
