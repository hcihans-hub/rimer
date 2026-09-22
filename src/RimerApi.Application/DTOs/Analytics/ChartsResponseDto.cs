namespace RimerApi.Application.DTOs.Analytics;

public class ChartsResponseDto
{
    public int TotalTickets { get; set; }
    public DateFilterDto DateFiltered { get; set; } = new();
    public List<CategoryCountDto> ByCategory { get; set; } = new();
    public List<DepartmentCountDto> ByDepartment { get; set; } = new();
    public List<DepartmentPerformanceDto> DepartmentPerformance { get; set; } = new();
    public StatusSummaryDto StatusSummary { get; set; } = new();
    public AgingDto Aging { get; set; } = new();
}

public class DateFilterDto
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}

public class CategoryCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DepartmentCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DepartmentPerformanceDto
{
    public string Name { get; set; } = string.Empty;
    
    // ── Raw metrics ───────────────────────────────
    public int TotalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int OverdueTickets { get; set; }       // 45+ days without resolution
    public int CriticalTickets { get; set; }      // 90+ days without resolution
    public int TransferredOutCount { get; set; }  // Tickets transferred away from this dept
    
    // ── Calculated rates (%) ──────────────────────
    public double ResolutionRate { get; set; }    // Resolved / Total * 100
    public double OverdueRate { get; set; }       // Overdue / Total * 100
    public double TransferRate { get; set; }      // TransferredOut / Total * 100
    
    // ── Composite score (0-100) ───────────────────
    public double PerformanceScore { get; set; }
    
    // ── Status color: green / yellow / orange / red
    public string ScoreColor { get; set; } = "green";
    public string ScoreLabel { get; set; } = "Mükemmel";
}

public class StatusSummaryDto
{
    public int Resolved { get; set; }
    public int Unanswered { get; set; }
    public int Pending { get; set; }
}

public class AgingDto
{
    public int Over45 { get; set; }
    public int Over60 { get; set; }
    public int Over90 { get; set; }
    public int Over180 { get; set; }
    public List<string> CriticalDepartments { get; set; } = new();
}

/// <summary>
/// Category breakdown for a specific department type (İdari/Akademik/İşçi/Öğrenci/Diğer).
/// </summary>
public class StaffTypeCategoryDto
{
    public string TypeName { get; set; } = string.Empty;
    public int Complaint { get; set; }
    public int Suggestion { get; set; }
    public int Request { get; set; }
    public int Thanks { get; set; }
    public int Info { get; set; }
    public int Total { get; set; }
}
