namespace RimerApi.Application.DTOs.Department;

/// <summary>
/// DTO for updating an existing department's name, description, and type.
/// </summary>
public class UpdateDepartmentDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
