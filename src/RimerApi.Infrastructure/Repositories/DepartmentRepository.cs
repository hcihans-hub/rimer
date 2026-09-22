using Microsoft.EntityFrameworkCore;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;

namespace RimerApi.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for department-specific data access operations.
/// </summary>
public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Department?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(d => d.Tickets.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Department>> GetActiveDepartmentsAsync()
    {
        return await DbSet
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name)
    {
        return await DbSet.AnyAsync(d =>
            d.Name.ToLower() == name.Trim().ToLower());
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Department> Items, int TotalCount)> GetPagedDepartmentsAsync(int page, int pageSize, bool onlyActive, string? searchTerm = null)
    {
        var query = DbSet.AsQueryable();

        if (onlyActive)
            query = query.Where(d => d.IsActive);
            
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(d => d.Name.Contains(searchTerm) || d.Description.Contains(searchTerm));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }
}
