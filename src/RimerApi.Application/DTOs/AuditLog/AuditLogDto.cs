using System;

namespace RimerApi.Application.DTOs.AuditLog;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    
    // Using object deserializes natively as JSON object in ASP.NET Core outputs
    public object? OldValues { get; set; }
    public object? NewValues { get; set; }
    
    // Display string instead of just id for the admin interface
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public DateTime Timestamp { get; set; }
}
