using RimerApi.Domain.Entities;

namespace RimerApi.Domain.Interfaces;

/// <summary>
/// Repository interface for department-specific data access operations.
/// </summary>
public interface IDepartmentRepository : IRepository<Department>
{
    /// <summary>
    /// Get a department by id with its users and tickets loaded.
    /// </summary>
    Task<Department?> GetByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// Get all active departments.
    /// </summary>
    Task<IEnumerable<Department>> GetActiveDepartmentsAsync();

    /// <summary>
    /// Check whether a department name is already taken (case-insensitive).
    /// </summary>
    Task<bool> NameExistsAsync(string name);

    /// <summary>
    /// Get paginated departments.
    /// </summary>
    Task<(IEnumerable<Department> Items, int TotalCount)> GetPagedDepartmentsAsync(int page, int pageSize, bool onlyActive, string? searchTerm = null);
}
