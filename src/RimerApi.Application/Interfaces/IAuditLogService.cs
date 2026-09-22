using System;
using System.Threading.Tasks;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.AuditLog;

namespace RimerApi.Application.Interfaces;

public interface IAuditLogService
{
    Task<ServiceResult<PagedResult<AuditLogDto>>> GetListAsync(AuditLogFilterDto filter);
}
