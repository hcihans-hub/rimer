using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.User;

/// <summary>
/// DTO for updating a user's profile (name, email, department assignment).
/// </summary>
public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
}
