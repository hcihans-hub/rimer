using RimerApi.Application.DTOs.User;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Abstraction for user lookup operations.
/// Implemented in the Infrastructure layer using UserManager&lt;ApplicationUser&gt;.
///
/// This interface exists so the Application layer never touches ASP.NET Identity directly.
/// Services call IUserService to resolve user ids into display-friendly UserDtos.
/// </summary>
public interface IUserService
{
    /// <summary>Get a user by their unique identifier.</summary>
    /// <returns>UserDto or null if the user doesn't exist.</returns>
    Task<UserDto?> GetByIdAsync(Guid userId, bool includeDeleted = false);

    /// <summary>Get a user by their email address.</summary>
    Task<UserDto?> GetByEmailAsync(string email);

    /// <summary>Check whether a user with the given id exists.</summary>
    Task<bool> ExistsAsync(Guid userId);

    /// <summary>
    /// Batch-resolve a set of user ids into UserDtos.
    /// Used to efficiently resolve creator/assignee names for ticket lists.
    /// </summary>
    /// <param name="userIds">The set of user ids to resolve.</param>
    /// <returns>Dictionary mapping user id to UserDto.</returns>
    Task<Dictionary<Guid, UserDto>> GetByIdsAsync(IEnumerable<Guid> userIds, bool includeDeleted = false);

    /// <summary>Search for users by full name or email.</summary>
    Task<List<Guid>> SearchUserIdsAsync(string searchTerm);
}
