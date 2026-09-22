using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Interfaces;
using RimerApi.Domain.Entities;

namespace RimerApi.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IUserService _userService;

    public AuditLogService(IAuditLogRepository repository, IUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    public async Task<ServiceResult<PagedResult<AuditLogDto>>> GetListAsync(AuditLogFilterDto filter)
    {
        var (items, totalCount) = await _repository.GetFilteredAsync(
            filter.TableName,
            filter.RecordId,
            filter.UserId,
            filter.FromDate,
            filter.ToDate,
            filter.PageNumber,
            filter.PageSize);
            
        var logList = items.ToList();

        // 3. Resolve Users
        var userIds = logList.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct();
        var usersMap = await _userService.GetByIdsAsync(userIds, includeDeleted: true);

        // 4. Map to DTOs
        var dtos = logList.Select(item => new AuditLogDto
        {
            Id = item.Id,
            TableName = item.TableName,
            RecordId = item.RecordId,
            Action = item.Action,
            UserId = item.UserId,
            UserName = item.UserId.HasValue ? (usersMap.GetValueOrDefault(item.UserId.Value)?.FullName ?? "Unknown User") : "System",
            OldValues = DeserializeJson(item.OldValues),
            NewValues = DeserializeJson(item.NewValues),
            Timestamp = item.CreatedAt
        }).ToList();

        var result = new PagedResult<AuditLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return ServiceResult<PagedResult<AuditLogDto>>.Success(result);
    }

    private static object? DeserializeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return json; // Fallback to raw string
        }
    }
}
