using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.User;

public class CreateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public Guid? DepartmentId { get; set; }
}
