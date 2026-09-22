using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;

namespace RimerApi.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetFilteredAsync(
        string? tableName, 
        string? recordId, 
        Guid? userId, 
        DateTime? fromDate, 
        DateTime? toDate, 
        int pageNumber, 
        int pageSize)
    {
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tableName))
            query = query.Where(x => x.TableName == tableName);

        if (!string.IsNullOrWhiteSpace(recordId))
            query = query.Where(x => x.RecordId == recordId);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.CreatedAt <= toDate.Value);

        query = query.OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
