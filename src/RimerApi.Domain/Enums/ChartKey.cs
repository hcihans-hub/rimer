namespace RimerApi.Domain.Enums;

/// <summary>
/// Available chart types in the analytics dashboard.
/// </summary>
public enum ChartKey
{
    TotalTickets = 0,
    CategoryDistribution = 1,
    DepartmentRanking = 2,
    StatusSummary = 3,
    AgingAnalysis = 4,
    DepartmentPerformance = 5,
    SatisfactionByDepartment = 6,
    ComplaintByDepartment = 7,
    StaffTypeDistribution = 8,
    AverageResponseTime = 9,
    MonthlyVolumeTrend = 10,
    PriorityStatusMatrix = 11
}
