using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;

namespace RimerApi.Domain.Interfaces;

/// <summary>
/// Repository interface for ticket-specific data access operations.
/// User details (creator name, assignee name) are resolved via UserManager in the service layer.
/// </summary>
public interface ITicketRepository : IRepository<Ticket>
{
    /// <summary>
    /// Get a ticket by id with Department and Histories eagerly loaded.
    /// </summary>
    Task<Ticket?> GetByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// Get a filtered, paginated list of tickets.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="departmentId">Optional department filter.</param>
    /// <param name="creatorId">Optional creator user id filter.</param>
    /// <param name="assignedToId">Optional assignee user id filter.</param>
    /// <param name="searchTerm">Optional search term to match against title or description.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetFilteredAsync(
        TicketStatus? status,
        TicketCategory? category,
        Guid? departmentId,
        Guid? creatorId,
        Guid? assignedToId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        bool excludeClosed = false,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get all tickets routed to a specific department.
    /// </summary>
    Task<IEnumerable<Ticket>> GetByDepartmentAsync(Guid departmentId);

    /// <summary>
    /// Get all tickets created by a specific user.
    /// </summary>
    Task<IEnumerable<Ticket>> GetByCreatorAsync(Guid creatorId);

    /// <summary>
    /// Generates a new unique human-readable reference number (e.g. RIM-2026-0001).
    /// </summary>
    Task<string> GenerateReferenceNoAsync();
}
