using System;
using RimerApi.Application.Common;

namespace RimerApi.Application.DTOs.AuditLog;

public class AuditLogFilterDto
{
    public string? TableName { get; set; }
    public string? RecordId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
