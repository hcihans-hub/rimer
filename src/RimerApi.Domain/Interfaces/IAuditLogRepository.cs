using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimerApi.Domain.Entities;

namespace RimerApi.Domain.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetFilteredAsync(
        string? tableName, 
        string? recordId, 
        Guid? userId, 
        DateTime? fromDate, 
        DateTime? toDate, 
        int pageNumber, 
        int pageSize);
}
