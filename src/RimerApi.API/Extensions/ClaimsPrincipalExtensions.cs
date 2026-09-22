using RimerApi.Application.Common;
using System.Security.Claims;

namespace RimerApi.API.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to simplify
/// JWT claim access across controllers. Centralizes all claim key
/// references so changes only need to happen in one place.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Map current identity claims to the Application layer's CurrentUser model.
    /// </summary>
    public static CurrentUser GetCurrentUser(this ClaimsPrincipal principal)
    {
        return new CurrentUser
        {
            Id = principal.GetUserId(),
            Role = principal.GetUserRole(),
            DepartmentId = principal.GetDepartmentId()
        };
    }
    /// <summary>
    /// Get the authenticated user's Id (Guid) from the JWT NameIdentifier claim.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the claim is missing or invalid.</exception>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("User id claim is missing from the token.");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("User id claim is not a valid GUID.");

        return userId;
    }

    /// <summary>
    /// Get the authenticated user's email from the JWT Email claim.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the claim is missing.</exception>
    public static string GetUserEmail(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("Email claim is missing from the token.");

        return value;
    }

    /// <summary>
    /// Get the authenticated user's role from the JWT Role claim.
    /// Returns "Student" as a safe default if the claim is missing.
    /// </summary>
    public static string GetUserRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Role) ?? "Student";
    }

    /// <summary>
    /// Get the user's department id from the "departmentId" custom claim.
    /// Returns null if the user has no department (Admin, Student without dept, etc.).
    /// </summary>
    public static Guid? GetDepartmentId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("departmentId");
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

