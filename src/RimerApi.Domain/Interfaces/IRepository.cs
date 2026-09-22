using System.Linq.Expressions;

namespace RimerApi.Domain.Interfaces;

/// <summary>
/// Generic repository interface providing standard CRUD operations for all entities.
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseEntity.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Get an entity by its unique identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, bool includeDeleted = false);

    /// <summary>Get all entities.</summary>
    Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false);

    /// <summary>Get queryable directly for complex filtering in services.</summary>
    IQueryable<T> GetQueryable();

    /// <summary>Find entities matching a predicate.</summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false);

    /// <summary>Get a paginated list of entities.</summary>
    /// <param name="predicate">Optional filter predicate.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? predicate, int pageNumber, int pageSize);

    /// <summary>Add a new entity.</summary>
    Task<T> AddAsync(T entity);

    /// <summary>Update an existing entity.</summary>
    void Update(T entity);

    /// <summary>Delete an entity.</summary>
    void Delete(T entity);

    /// <summary>Physically remove all matching rows from the database, bypassing soft-delete.</summary>
    Task<int> HardDeleteRangeAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Check if any entity matches the given predicate.</summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Count entities matching the given predicate.</summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Persist all pending changes to the database.</summary>
    Task<int> SaveChangesAsync();
}
