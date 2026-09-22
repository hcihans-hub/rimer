using Microsoft.EntityFrameworkCore;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;

namespace RimerApi.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ticket-specific data access operations.
/// User details are resolved separately via UserManager — only Guid FKs here.
/// </summary>
public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(t => t.Department)
            .Include(t => t.AssignedDepartment)
            .Include(t => t.Histories.OrderByDescending(h => h.ChangedAt))
            .Include(t => t.Transfers.OrderByDescending(tr => tr.CreatedAt))
                .ThenInclude(tr => tr.FromDepartment)
            .Include(t => t.Transfers)
                .ThenInclude(tr => tr.ToDepartment)
            .Include(t => t.Replies.OrderBy(r => r.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Ticket> Items, int TotalCount)> GetFilteredAsync(
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
        DateTime? endDate = null)
    {
        var query = DbSet
            .Include(t => t.Department)
            .Include(t => t.AssignedDepartment)
            .AsQueryable();

        // ── Apply Filters ──────────────────────────────────────────
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        // departmentId filter now targets AssignedDepartmentId (authoritative)
        if (departmentId.HasValue)
            query = query.Where(t => t.AssignedDepartmentId == departmentId.Value);

        if (creatorId.HasValue)
            query = query.Where(t => t.CreatorId == creatorId.Value);

        if (assignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == assignedToId.Value);

        if (excludeClosed)
            query = query.Where(t => t.Status != TicketStatus.Closed);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value.ToUniversalTime());

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term) ||
                t.ReferenceNo.ToLower().Contains(term));
        }

        // ── Count & Paginate ───────────────────────────────────────
        var totalCount = await query.CountAsync();

        // Sort: recently transferred first (priority signal), then by creation date
        var items = await query
            .OrderByDescending(t => t.LastTransferredAt.HasValue)
            .ThenByDescending(t => t.LastTransferredAt)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Ticket>> GetByDepartmentAsync(Guid departmentId)
    {
        return await DbSet
            .Where(t => t.AssignedDepartmentId == departmentId)
            .OrderByDescending(t => t.LastTransferredAt.HasValue)
            .ThenByDescending(t => t.LastTransferredAt)
            .ThenByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Ticket>> GetByCreatorAsync(Guid creatorId)
    {
        return await DbSet
            .Include(t => t.Department)
            .Include(t => t.AssignedDepartment)
            .Where(t => t.CreatorId == creatorId)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<string> GenerateReferenceNoAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RIM-{year}-";

        // Query the database for the last issued reference number for the current year
        var lastTicketRef = await DbSet
            .Where(t => t.ReferenceNo.StartsWith(prefix))
            .OrderByDescending(t => t.ReferenceNo)
            .Select(t => t.ReferenceNo)
            .FirstOrDefaultAsync();

        var nextSequence = 1;
        if (lastTicketRef != null)
        {
            var parts = lastTicketRef.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var currentMax))
            {
                nextSequence = currentMax + 1;
            }
        }

        return $"{prefix}{nextSequence:D6}";
    }
}

