namespace RimerApi.Application.DTOs.Department;

/// <summary>
/// DTO for creating a new department.
/// </summary>
public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
