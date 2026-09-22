using RimerApi.Application.Common;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RimerApi.Application.Services;

public class SystemLogService : ISystemLogService
{
    private readonly IRepository<SystemLog> _logRepository;
    private readonly IUserService _userService;

    public SystemLogService(IRepository<SystemLog> logRepository, IUserService userService)
    {
        _logRepository = logRepository;
        _userService = userService;
    }

    public async Task LogAsync(string? userId, string action, string entity, string? entityId = null, string? details = null)
    {
        var log = new SystemLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        await _logRepository.AddAsync(log);
        await _logRepository.SaveChangesAsync();
    }

    public async Task<ServiceResult<PagedResult<SystemLogDto>>> GetLogsAsync(
        int page = 1, 
        int pageSize = 20,
        string? action = null,
        string? userName = null,
        string? details = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = await _logRepository.GetAllAsync();
        
        var logs = query.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            logs = logs.Where(x => x.Action.Contains(action, StringComparison.OrdinalIgnoreCase));
            
        if (!string.IsNullOrWhiteSpace(details))
            logs = logs.Where(x => (x.Details != null && x.Details.Contains(details, StringComparison.OrdinalIgnoreCase)) || 
                                   (x.EntityId != null && x.EntityId.Contains(details, StringComparison.OrdinalIgnoreCase)));
                                   
        if (startDate.HasValue)
            logs = logs.Where(x => x.Timestamp >= startDate.Value);
            
        if (endDate.HasValue)
            logs = logs.Where(x => x.Timestamp <= endDate.Value);
            
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var matchedUserIds = await _userService.SearchUserIdsAsync(userName);
            var stringIds = matchedUserIds.Select(id => id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            
            logs = logs.Where(x => x.UserId != null && stringIds.Contains(x.UserId));
        }
        
        logs = logs.OrderByDescending(x => x.Timestamp);
        
        var totalCount = logs.Count();
        
        var items = logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SystemLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Action = x.Action,
                Entity = x.Entity,
                EntityId = x.EntityId,
                Details = x.Details,
                Timestamp = x.Timestamp
            })
            .ToList();

        // Resolve user names for both UserId and EntityId (when Entity == "User")
        var userIdsToFetch = items
            .Where(x => !string.IsNullOrEmpty(x.UserId) && Guid.TryParse(x.UserId, out _))
            .Select(x => Guid.Parse(x.UserId!))
            .ToHashSet();

        var entityUserIdsToFetch = items
            .Where(x => x.Entity == "User" && !string.IsNullOrEmpty(x.EntityId) && Guid.TryParse(x.EntityId, out _))
            .Select(x => Guid.Parse(x.EntityId!));
            
        userIdsToFetch.UnionWith(entityUserIdsToFetch);

        if (userIdsToFetch.Any())
        {
            var userMap = await _userService.GetByIdsAsync(userIdsToFetch, includeDeleted: true);
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.UserId) && Guid.TryParse(item.UserId, out var uid))
                {
                    if (userMap.TryGetValue(uid, out var userDto))
                    {
                        item.UserName = userDto.FullName ?? userDto.Email;
                    }
                }

                if (item.Entity == "User" && string.IsNullOrEmpty(item.Details) && !string.IsNullOrEmpty(item.EntityId) && Guid.TryParse(item.EntityId, out var targetUid))
                {
                    if (userMap.TryGetValue(targetUid, out var targetDto))
                    {
                        item.Details = $"Hedef Kullanıcı: {targetDto.FullName ?? targetDto.Email}";
                    }
                }
            }
        }

        var pagedResult = new PagedResult<SystemLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<SystemLogDto>>.Success(pagedResult);
    }
}
