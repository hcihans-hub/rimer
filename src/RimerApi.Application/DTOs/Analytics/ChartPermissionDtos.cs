using RimerApi.Domain.Enums;

namespace RimerApi.Application.DTOs.Analytics;

/// <summary>
/// Helper for chart key operations.
/// </summary>
public static class ChartKeyHelper
{
    public static readonly ChartKey[] All = Enum.GetValues<ChartKey>();

    /// <summary>Maps enum to the JSON key used in the response dictionary.</summary>
    public static string ToJsonKey(this ChartKey key) => key switch
    {
        ChartKey.TotalTickets => "totalTickets",
        ChartKey.CategoryDistribution => "categoryDistribution",
        ChartKey.DepartmentRanking => "departmentRanking",
        ChartKey.StatusSummary => "statusSummary",
        ChartKey.AgingAnalysis => "agingAnalysis",
        ChartKey.DepartmentPerformance => "departmentPerformance",
        ChartKey.SatisfactionByDepartment => "satisfactionByDepartment",
        ChartKey.ComplaintByDepartment => "complaintByDepartment",
        ChartKey.StaffTypeDistribution => "staffTypeDistribution",
        ChartKey.AverageResponseTime => "averageResponseTime",
        ChartKey.MonthlyVolumeTrend => "monthlyVolumeTrend",
        ChartKey.PriorityStatusMatrix => "priorityStatusMatrix",
        _ => key.ToString()
    };

    /// <summary>Try parse a string to ChartKey enum.</summary>
    public static bool TryParse(string value, out ChartKey result)
    {
        // Try direct enum parse first
        if (Enum.TryParse(value, ignoreCase: true, out result))
            return true;

        // Try JSON key mapping
        result = value.ToLower() switch
        {
            "totaltickets" => ChartKey.TotalTickets,
            "categorydistribution" => ChartKey.CategoryDistribution,
            "departmentranking" => ChartKey.DepartmentRanking,
            "statussummary" => ChartKey.StatusSummary,
            "aginganalysis" => ChartKey.AgingAnalysis,
            "departmentperformance" => ChartKey.DepartmentPerformance,
            "satisfactionbydepartment" => ChartKey.SatisfactionByDepartment,
            "complaintbydepartment" => ChartKey.ComplaintByDepartment,
            "stafftypedistribution" => ChartKey.StaffTypeDistribution,
            "averageresponsetime" => ChartKey.AverageResponseTime,
            "monthlyvolumetrend" => ChartKey.MonthlyVolumeTrend,
            "prioritystatusmatrix" => ChartKey.PriorityStatusMatrix,
            _ => (ChartKey)(-1)
        };

        return (int)result >= 0;
    }
}

/// <summary>
/// Request body for assigning chart permissions.
/// </summary>
public class AssignChartPermissionsDto
{
    public Guid UserId { get; set; }
    public List<string> ChartKeys { get; set; } = new();
}

/// <summary>
/// Response for permission-filtered charts.
/// </summary>
public class PermissionFilteredChartsDto
{
    public DateFilterDto DateFiltered { get; set; } = new();
    public Dictionary<string, object> Charts { get; set; } = new();
}
