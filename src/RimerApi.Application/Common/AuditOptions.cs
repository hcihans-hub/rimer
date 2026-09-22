using System;

namespace RimerApi.Application.Common;

public class AuditOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxFieldLength { get; set; } = 200;
    public int MaxJsonLength { get; set; } = 85000; // < 85KB to avoid LOH (Large Object Heap) allocation
    public int MaxPayloadSize { get; set; } = 85000; // Legacy mapping 
    public int BatchSize { get; set; } = 100;
    public int RetentionDays { get; set; } = 30;
    public string[] ExcludedFields { get; set; } = Array.Empty<string>();
}
