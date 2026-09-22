namespace RimerApi.Application.DTOs.Dashboard;

/// <summary>
/// Complete dashboard statistics snapshot for the admin panel.
/// </summary>
public class DashboardStatsDto
{
    // ── Ticket Overview ────────────────────────────────────────────
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ClosedTickets { get; set; }

    /// <summary>Percentage of tickets resolved (Closed / Total * 100).</summary>
    public double ResolutionRate { get; set; }

    // ── Tickets by Category ────────────────────────────────────────
    public int ComplaintTickets { get; set; }
    public int SuggestionTickets { get; set; }
    public int RequestTickets { get; set; }
    public int ThanksTickets { get; set; }

    // ── Tickets by Priority ────────────────────────────────────────
    public int LowPriorityTickets { get; set; }
    public int MediumPriorityTickets { get; set; }
    public int HighPriorityTickets { get; set; }

    // ── User Overview ──────────────────────────────────────────────
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalStaff { get; set; }
    public int TotalAdmins { get; set; }

    // ── Department Breakdown ───────────────────────────────────────
    /// <summary>Ticket counts per department.</summary>
    public List<DepartmentStatDto> TicketsByDepartment { get; set; } = [];

    // ── Trend: Last 14 Days ────────────────────────────────────────
    /// <summary>Daily ticket creation counts for the last 14 days.</summary>
    public List<DailyTicketStatDto> Last14DaysTrend { get; set; } = [];

    // ── Recent Activity ────────────────────────────────────────────
    /// <summary>The 5 most recently created tickets.</summary>
    public List<RecentTicketDto> RecentTickets { get; set; } = [];
}

public class DepartmentStatDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
}

public class DailyTicketStatDto
{
    public string Date { get; set; } = string.Empty;   // "MMM dd" format, e.g. "Apr 10"
    public int Count { get; set; }
}

public class RecentTicketDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}
