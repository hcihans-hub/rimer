using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;

namespace RimerApi.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing standard CRUD operations via EF Core.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, bool includeDeleted = false)
    {
        var query = DbSet.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false)
    {
        var query = DbSet.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        
        return await query.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public virtual IQueryable<T> GetQueryable()
    {
        return DbSet.AsQueryable();
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false)
    {
        var query = DbSet.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        
        return await query.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<T, bool>>? predicate, int pageNumber, int pageSize)
    {
        var query = DbSet.AsNoTracking();
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Default order by CreatedAt desc if it's a BaseEntity
        if (typeof(T).IsSubclassOf(typeof(Domain.Entities.BaseEntity)))
        {
            query = query.OrderByDescending(e => EF.Property<DateTime>(e, "CreatedAt"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity)
    {
        var entry = await DbSet.AddAsync(entity);
        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void Delete(T entity)
    {
        if (Context.Entry(entity).State == EntityState.Detached)
        {
            DbSet.Attach(entity);
        }
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual async Task<int> HardDeleteRangeAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.IgnoreQueryFilters().Where(predicate).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    /// <inheritdoc />
    public virtual async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync();
    }
}
