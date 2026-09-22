using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.User;

/// <summary>
/// Payload to change a user's role. Admin only.
/// </summary>
public class UpdateUserRoleDto
{
    /// <summary>New role to assign to the user.</summary>
    public UserRole Role { get; set; }
}
