using System;

namespace RimerApi.Application.DTOs.Analytics;

public class UserChartPermissionDto
{
    public Guid UserId { get; set; }
    public string ChartKey { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
