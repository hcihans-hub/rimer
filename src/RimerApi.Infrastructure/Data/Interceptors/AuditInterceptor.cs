using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Infrastructure.Data.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogQueueService _queueService;
    private readonly AuditOptions _options;
    private readonly ILogger<AuditInterceptor> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, WriteIndented = false };

    private readonly ConditionalWeakTable<DbContext, List<AuditLogMessageDto>> _pendingAudits = new();

    public AuditInterceptor(
        IHttpContextAccessor httpContextAccessor,
        IAuditLogQueueService queueService,
        IOptions<AuditOptions> options,
        ILogger<AuditInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _queueService = queueService;
        _options = options.Value;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { PrepareAuditLogs(eventData.Context); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to prepare audit logs."); }
        }
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { PrepareAuditLogs(eventData.Context); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to prepare audit logs async."); }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { ProcessSaved(eventData.Context, null); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to commit audit logs."); }
        }
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { ProcessSaved(eventData.Context, null); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to commit audit logs async."); }
        }
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { ProcessSaved(eventData.Context, eventData.Exception); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to update rejected audit logs."); }
        }
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (_options.Enabled && eventData.Context != null)
        {
            try { ProcessSaved(eventData.Context, eventData.Exception); } 
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to update rejected audit logs async."); }
        }
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void PrepareAuditLogs(DbContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdString = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : null;

        var priority = RimerApi.Application.Enums.RequestPriority.Normal;
        var endpoint = httpContext?.GetEndpoint();
        var priorityAttribute = endpoint?.Metadata.GetMetadata<RimerApi.Application.Attributes.RequestPriorityAttribute>();
        if (priorityAttribute != null) priority = priorityAttribute.Priority;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog && 
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0) return;

        var logs = new List<AuditLogMessageDto>();
        var now = DateTime.UtcNow;
        var sequence = 1;

        foreach (var entry in entries)
        {
            // HARD GUARANTEE: Never process AuditLog entity to avoid infinite loop
            if (entry.Entity is AuditLog) continue;

            var correlationId = GenerateSequentialGuid(); // COMB sequential Guid to prevent index fragmentation

            var auditLog = new AuditLogMessageDto
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                UserId = userId,
                Action = "Unknown",
                Status = "Attempted",
                Timestamp = now,
                CorrelationId = correlationId,
                OccurredAt = now,
                Sequence = sequence++,
                Priority = priority
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            bool isSoftDelete = false;
            // Catch soft deletes mapped as modifications
            if (entry.Entity is ISoftDeletable && entry.State == EntityState.Modified)
            {
                var isDeletedProp = entry.Property("IsDeleted");
                if (isDeletedProp.IsModified && isDeletedProp.CurrentValue is true) isSoftDelete = true;
            }

            if (entry.State == EntityState.Added)
            {
                auditLog.Action = "CREATE";
                foreach (var prop in entry.Properties)
                {
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Deleted || isSoftDelete)
            {
                auditLog.Action = "DELETE";
                foreach (var prop in entry.Properties)
                {
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                auditLog.Action = "UPDATE";
                foreach (var prop in entry.Properties.Where(p => p.IsModified))
                {
                    if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;

                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                }

                if (oldValues.Count == 0 && newValues.Count == 0) continue;
            }

            var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            auditLog.RecordId = idProperty?.CurrentValue?.ToString() ?? "Unknown";

            // Application-level Masking BEFORE Serialization
            var maskedOld = MaskingUtility.MaskDictionary(oldValues, _options.MaxFieldLength);
            var maskedNew = MaskingUtility.MaskDictionary(newValues, _options.MaxFieldLength);

            // Optimize and protect from maximum payload memory bounds (< 85KB LOH safeguard)
            string oldJson = maskedOld.Count == 0 ? "{}" : JsonSerializer.Serialize(maskedOld, _jsonOptions);
            string newJson = maskedNew.Count == 0 ? "{}" : JsonSerializer.Serialize(maskedNew, _jsonOptions);

            if (oldJson.Length > _options.MaxJsonLength || newJson.Length > _options.MaxJsonLength)
            {
                _logger.LogWarning("Audit log {Action} payload exceeded limit of {MaxJsonLength} characters. Payload truncated to structured object.", auditLog.Action, _options.MaxJsonLength);
                auditLog.OldValues = $"{{\"truncated\":true,\"originalSize\":{oldJson.Length}}}";
                auditLog.NewValues = $"{{\"truncated\":true,\"originalSize\":{newJson.Length}}}";
            }
            else
            {
                auditLog.OldValues = oldJson;
                auditLog.NewValues = newJson;
            }

            // Serilog Integration (Structured Logging) - STRICTLY OMIT PAYLOAD FIELDS
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                _logger.LogInformation("Audit {Action} recorded for {Table} / {RecordId} (Sequence: {Sequence})", 
                    auditLog.Action, auditLog.TableName, auditLog.RecordId, auditLog.Sequence);
            }

            logs.Add(auditLog);
        }

        if (logs.Any())
        {
            _pendingAudits.AddOrUpdate(context, logs);
        }
    }

    private void ProcessSaved(DbContext context, Exception? exception)
    {
        if (_pendingAudits.TryGetValue(context, out var logs))
        {
            foreach (var log in logs)
            {
                if (exception == null)
                {
                    log.Status = "Success";
                }
                else
                {
                    log.Status = "Failed";
                    var msg = exception.Message;
                    log.ErrorMessage = msg.Length > 200 ? msg.Substring(0, 200) + "..." : msg;
                }
            }

            _queueService.EnqueueAuditLogs(logs);
            _pendingAudits.Remove(context);
        }
    }

    private static Guid GenerateSequentialGuid()
    {
        byte[] guidArray = Guid.NewGuid().ToByteArray();
        DateTime baseDate = new DateTime(1900, 1, 1);
        DateTime now = DateTime.UtcNow;
        TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
        TimeSpan msecs = now.TimeOfDay;
        byte[] daysArray = BitConverter.GetBytes(days.Days);
        byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));
        Array.Reverse(daysArray);
        Array.Reverse(msecsArray);
        Array.Copy(daysArray, daysArray.Length - 2, guidArray, 10, 2);
        Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, 12, 4);
        return new Guid(guidArray);
    }
}
