using System;

namespace RimerApi.Application.DTOs.AuditLog;

/// <summary>
/// Lightweight payload for Hangfire background audit log insertion.
/// </summary>
public class AuditLogMessageDto
{
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = "Attempted";
    public string? ErrorMessage { get; set; }
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public Guid? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid CorrelationId { get; set; }
    public int Sequence { get; set; }
    public DateTime OccurredAt { get; set; }
    public RimerApi.Application.Enums.RequestPriority Priority { get; set; } = RimerApi.Application.Enums.RequestPriority.Normal;
}
