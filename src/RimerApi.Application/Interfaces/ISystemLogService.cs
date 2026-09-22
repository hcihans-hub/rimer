using RimerApi.Application.Common;
using System;
using System.Threading.Tasks;

namespace RimerApi.Application.Interfaces;

public interface ISystemLogService
{
    Task LogAsync(string? userId, string action, string entity, string? entityId = null, string? details = null);
    Task<ServiceResult<PagedResult<SystemLogDto>>> GetLogsAsync(
        int page = 1, 
        int pageSize = 20,
        string? action = null,
        string? userName = null,
        string? details = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
}

public class SystemLogDto
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
