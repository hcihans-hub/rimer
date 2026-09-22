using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Dashboard;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Enums;
using RimerApi.Infrastructure.Data;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// IDashboardService implementation.
/// Runs all queries in parallel for maximum performance.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DashboardStatsDto>> GetStatsAsync()
    {
        var ticketQuery = _context.Tickets.AsNoTracking();

        // ── Run all ticket aggregations in parallel ─────────────────
        var totalTask         = ticketQuery.CountAsync();
        var openTask          = ticketQuery.CountAsync(t => t.Status == TicketStatus.Submitted);
        var inProgressTask    = ticketQuery.CountAsync(t => t.Status == TicketStatus.Reviewing || t.Status == TicketStatus.WaitingDepartment);
        var closedTask        = ticketQuery.CountAsync(t => t.Status == TicketStatus.Closed);
        var complaintTask     = ticketQuery.CountAsync(t => t.Category == TicketCategory.Complaint);
        var suggestionTask    = ticketQuery.CountAsync(t => t.Category == TicketCategory.Suggestion);
        var requestTask       = ticketQuery.CountAsync(t => t.Category == TicketCategory.Request);
        var thanksTask        = ticketQuery.CountAsync(t => t.Category == TicketCategory.Thanks);
        var lowPriorityTask   = ticketQuery.CountAsync(t => t.Priority == TicketPriority.Low);
        var medPriorityTask   = ticketQuery.CountAsync(t => t.Priority == TicketPriority.Normal);
        var highPriorityTask  = ticketQuery.CountAsync(t => t.Priority == TicketPriority.Important || t.Priority == TicketPriority.Critical);

        // ── Department breakdown ────────────────────────────────────
        var deptStatsTask = _context.Departments
            .AsNoTracking()
            .Select(d => new DepartmentStatDto
            {
                DepartmentName = d.Name,
                TotalTickets   = d.Tickets!.Count(),
                OpenTickets    = d.Tickets!.Count(t => t.Status == TicketStatus.Submitted),
                ClosedTickets  = d.Tickets!.Count(t => t.Status == TicketStatus.Closed)
            })
            .ToListAsync();

        // ── Last 14 days trend ──────────────────────────────────────
        var cutoff = DateTime.UtcNow.Date.AddDays(-13);
        var trendTask = ticketQuery
            .Where(t => t.CreatedAt >= cutoff)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        // ── Recent tickets (last 5) ─────────────────────────────────
        var recentTask = ticketQuery
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new RecentTicketDto
            {
                Id        = t.Id,
                Title     = t.Title,
                Category  = t.Category.ToString(),
                Status    = t.Status.ToString(),
                Priority  = (int)t.Priority,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        // ── User counts ─────────────────────────────────────────────
        var usersTask   = _userManager.Users.CountAsync();
        var studentsTask = _userManager.Users.CountAsync(u => u.Role == UserRole.Student);
        var staffTask    = _userManager.Users.CountAsync(u => u.Role == UserRole.Staff);
        var adminsTask   = _userManager.Users.CountAsync(u => u.Role == UserRole.Admin);

        // ── Await all ───────────────────────────────────────────────
        await Task.WhenAll(
            totalTask, openTask, inProgressTask, closedTask,
            complaintTask, suggestionTask, requestTask, thanksTask,
            lowPriorityTask, medPriorityTask, highPriorityTask,
            deptStatsTask, trendTask, recentTask,
            usersTask, studentsTask, staffTask, adminsTask);

        var total  = await totalTask;
        var closed = await closedTask;

        // ── Build 14-day trend (fill missing days with 0) ──────────
        var trendRaw    = await trendTask;
        var trendLookup = trendRaw.ToDictionary(x => x.Date, x => x.Count);
        var trend = Enumerable.Range(0, 14)
            .Select(i =>
            {
                var date = cutoff.AddDays(i);
                return new DailyTicketStatDto
                {
                    Date  = date.ToString("MMM dd"),
                    Count = trendLookup.TryGetValue(date, out var c) ? c : 0
                };
            })
            .ToList();

        var dto = new DashboardStatsDto
        {
            // Ticket Overview
            TotalTickets      = total,
            OpenTickets       = await openTask,
            InProgressTickets = await inProgressTask,
            ClosedTickets     = closed,
            ResolutionRate    = total == 0 ? 0 : Math.Round((double)closed / total * 100, 1),

            // Category
            ComplaintTickets  = await complaintTask,
            SuggestionTickets = await suggestionTask,
            RequestTickets    = await requestTask,
            ThanksTickets     = await thanksTask,

            // Priority
            LowPriorityTickets    = await lowPriorityTask,
            MediumPriorityTickets = await medPriorityTask,
            HighPriorityTickets   = await highPriorityTask,

            // Users
            TotalUsers    = await usersTask,
            TotalStudents = await studentsTask,
            TotalStaff    = await staffTask,
            TotalAdmins   = await adminsTask,

            // Department + Trend + Recent
            TicketsByDepartment = await deptStatsTask,
            Last14DaysTrend     = trend,
            RecentTickets       = await recentTask
        };

        return ServiceResult<DashboardStatsDto>.Success(dto);
    }
}
