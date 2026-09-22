using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Analytics;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Application.Services;

public class ChartPermissionService : IChartPermissionService
{
    private readonly IRepository<UserChartPermission> _permRepo;
    private readonly ITicketRepository _ticketRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserService _userService;

    public ChartPermissionService(
        IRepository<UserChartPermission> permRepo,
        ITicketRepository ticketRepository,
        IDepartmentRepository departmentRepository,
        IUserService userService)
    {
        _permRepo = permRepo;
        _ticketRepository = ticketRepository;
        _departmentRepository = departmentRepository;
        _userService = userService;
    }

    // ── Get all permissions (Admin only) ──────────────────────────────
    public async Task<ServiceResult<List<UserChartPermissionDto>>> GetAllPermissionsAsync(CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
            return ServiceResult<List<UserChartPermissionDto>>.Forbidden("Only Admin can view chart permissions.");

        var allPerms = await _permRepo.GetAllAsync();
        var dtos = allPerms.Select(p => new UserChartPermissionDto
        {
            UserId = p.UserId,
            ChartKey = p.ChartKey.ToJsonKey(),
            CreatedAt = p.CreatedAt
        }).ToList();

        return ServiceResult<List<UserChartPermissionDto>>.Success(dtos);
    }

    // ── Get permissions for a user (Admin only) ───────────────────
    public async Task<ServiceResult<List<string>>> GetPermissionsAsync(Guid userId, CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
            return ServiceResult<List<string>>.Forbidden("Only Admin can view chart permissions.");

        var perms = await _permRepo.FindAsync(p => p.UserId == userId);
        var keys = perms.Select(p => p.ChartKey.ToJsonKey()).ToList();
        return ServiceResult<List<string>>.Success(keys);
    }

    // ── Assign permissions (Admin only) ───────────────────────────
    public async Task<ServiceResult<List<string>>> AssignPermissionsAsync(AssignChartPermissionsDto dto, CurrentUser currentUser)
    {
        if (!currentUser.IsAdmin)
            return ServiceResult<List<string>>.Forbidden("Only Admin can assign chart permissions.");

        // Parse and validate chart keys from strings to enums
        var validKeys = new List<ChartKey>();
        foreach (var keyStr in dto.ChartKeys.Distinct())
        {
            if (ChartKeyHelper.TryParse(keyStr, out var parsed))
                validKeys.Add(parsed);
        }

        // Hard-delete existing permissions (bypasses soft-delete interceptor + unique index conflict)
        await _permRepo.HardDeleteRangeAsync(p => p.UserId == dto.UserId);

        // Add new permissions
        var utcNow = DateTime.UtcNow;
        foreach (var key in validKeys)
        {
            await _permRepo.AddAsync(new UserChartPermission
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                ChartKey = key,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        await _permRepo.SaveChangesAsync();
        return ServiceResult<List<string>>.Success(validKeys.Select(k => k.ToJsonKey()).ToList());
    }

    // ── Get permitted charts data ─────────────────────────────────
    public async Task<ServiceResult<PermissionFilteredChartsDto>> GetPermittedChartsAsync(
        DateTime? startDate, DateTime? endDate, CurrentUser currentUser)
    {
        // 1. Get user's allowed chart keys
        HashSet<ChartKey> allowedKeys;

        if (currentUser.IsAdmin || currentUser.IsRector)
        {
            // Admin and Rector get ALL charts automatically
            allowedKeys = ChartKeyHelper.All.ToHashSet();
        }
        else
        {
            var permsEnum = await _permRepo.FindAsync(p => p.UserId == currentUser.Id);
            allowedKeys = permsEnum.Select(p => p.ChartKey).ToHashSet();
        }

        if (allowedKeys.Count == 0)
            return ServiceResult<PermissionFilteredChartsDto>.Forbidden("No chart permissions assigned.");

        // 2. Fetch ticket data
        var ticketsEnum = await _ticketRepository.FindAsync(t =>
            (!startDate.HasValue || t.CreatedAt >= startDate.Value) &&
            (!endDate.HasValue || t.CreatedAt <= endDate.Value)
        );
        var tickets = ticketsEnum.ToList();
        var utcNow = DateTime.UtcNow;

        var result = new PermissionFilteredChartsDto
        {
            DateFiltered = new DateFilterDto { Start = startDate, End = endDate }
        };

        // 3. Build ONLY permitted chart data
        if (allowedKeys.Contains(ChartKey.TotalTickets))
        {
            result.Charts[ChartKey.TotalTickets.ToJsonKey()] = new { total = tickets.Count };
        }

        if (allowedKeys.Contains(ChartKey.CategoryDistribution))
        {
            result.Charts[ChartKey.CategoryDistribution.ToJsonKey()] = new List<CategoryCountDto>
            {
                new() { Name = "complaint",  Count = tickets.Count(t => t.Category == TicketCategory.Complaint) },
                new() { Name = "suggestion", Count = tickets.Count(t => t.Category == TicketCategory.Suggestion) },
                new() { Name = "request",    Count = tickets.Count(t => t.Category == TicketCategory.Request) },
                new() { Name = "thanks",     Count = tickets.Count(t => t.Category == TicketCategory.Thanks) },
                new() { Name = "info",       Count = tickets.Count(t => t.Category == TicketCategory.InfoRequest || t.Category == TicketCategory.Question) }
            };
        }

        var allDepts = await _departmentRepository.GetAllAsync();
        var deptMap = allDepts.ToDictionary(d => d.Id, d => d.Name);

        if (allowedKeys.Contains(ChartKey.DepartmentRanking))
        {
            result.Charts[ChartKey.DepartmentRanking.ToJsonKey()] = tickets
                .Select(t => t.AssignedDepartmentId ?? t.DepartmentId) // Prefer AssignedDepartmentId
                .Where(id => id.HasValue)
                .GroupBy(id => id!.Value)
                .Select(g => new DepartmentCountDto
                {
                    Name = deptMap.GetValueOrDefault(g.Key, "Unknown"),
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList();
        }

        if (allowedKeys.Contains(ChartKey.DepartmentPerformance))
        {
            // Count transfers per source department for transfer-rate metric
            var transferCounts = tickets
                .Where(t => t.LastTransferredAt.HasValue && t.DepartmentId.HasValue)
                .GroupBy(t => t.DepartmentId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            result.Charts[ChartKey.DepartmentPerformance.ToJsonKey()] = tickets
                .Where(t => (t.AssignedDepartmentId ?? t.DepartmentId).HasValue)
                .GroupBy(t => (t.AssignedDepartmentId ?? t.DepartmentId)!.Value)
                .Select(g => 
                {
                    var total = g.Count();
                    var resolved = g.Count(t => t.Status == TicketStatus.Closed);
                    var inProgress = g.Count(t => t.Status == TicketStatus.Reviewing || t.Status == TicketStatus.WaitingDepartment);
                    var overdue = g.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays > 45);
                    var critical = g.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays > 90);
                    var transferredOut = transferCounts.GetValueOrDefault(g.Key, 0);

                    var resolutionRate = total > 0 ? Math.Round((double)resolved / total * 100, 1) : 0;
                    var overdueRate = total > 0 ? Math.Round((double)overdue / total * 100, 1) : 0;
                    var transferRate = total > 0 ? Math.Round((double)transferredOut / total * 100, 1) : 0;

                    // ── Composite Performance Score (0-100) ──────────────
                    // Base: resolution rate contribution (max 50pts)
                    var score = resolutionRate * 0.50;
                    // Bonus: in-progress shows activity (max 10pts)
                    var activeRate = total > 0 ? (double)inProgress / total * 100 : 0;
                    score += Math.Min(activeRate * 0.10, 10);
                    // Base idle bonus: if nothing is overdue, add 25pts
                    var overdueRatio = total > 0 ? (double)overdue / total : 0;
                    score += (1.0 - overdueRatio) * 25;
                    // Critical penalty: up to -15pts
                    var criticalRatio = total > 0 ? (double)critical / total : 0;
                    score -= criticalRatio * 15;
                    // Transfer penalty: up to -10pts
                    var transferRatio = total > 0 ? (double)transferredOut / total : 0;
                    score -= transferRatio * 10;

                    score = Math.Round(Math.Clamp(score, 0, 100), 1);

                    var (color, label) = score switch
                    {
                        >= 85 => ("green", "Mükemmel"),
                        >= 70 => ("yellow", "İyi"),
                        >= 50 => ("orange", "Dikkat"),
                        _ => ("red", "Kritik")
                    };

                    return new DepartmentPerformanceDto
                    {
                        Name = deptMap.GetValueOrDefault(g.Key, "Unknown"),
                        TotalTickets = total,
                        ResolvedTickets = resolved,
                        InProgressTickets = inProgress,
                        OverdueTickets = overdue,
                        CriticalTickets = critical,
                        TransferredOutCount = transferredOut,
                        ResolutionRate = resolutionRate,
                        OverdueRate = overdueRate,
                        TransferRate = transferRate,
                        PerformanceScore = score,
                        ScoreColor = color,
                        ScoreLabel = label
                    };
                })
                .OrderByDescending(d => d.PerformanceScore)
                .ToList();
        }

        if (allowedKeys.Contains(ChartKey.StatusSummary))
        {
            // 1. Static summary for KPI cards
            result.Charts[ChartKey.StatusSummary.ToJsonKey()] = new 
            {
                resolved = tickets.Count(t => t.Status == TicketStatus.Closed),
                pending = tickets.Count(t => (t.Status == TicketStatus.Reviewing || t.Status == TicketStatus.WaitingDepartment) && (utcNow - t.CreatedAt).TotalDays <= 45),
                waiting = tickets.Count(t => t.Status == TicketStatus.Submitted && (utcNow - t.CreatedAt).TotalDays <= 45),
                unanswered = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays > 45)
            };

            // 2. Trend data for Stacked Area chart (Last 6 Months)
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => utcNow.AddMonths(-i))
                .OrderBy(d => d)
                .Select(d => new { Year = d.Year, Month = d.Month, Label = d.ToString("MMM") })
                .ToList();

            result.Charts["statusTrend"] = last6Months.Select(m => new {
                month = m.Label,
                resolved = tickets.Count(t => t.Status == TicketStatus.Closed && t.CreatedAt.Year == m.Year && t.CreatedAt.Month == m.Month),
                pending = tickets.Count(t => (t.Status == TicketStatus.Reviewing || t.Status == TicketStatus.WaitingDepartment) && (utcNow - t.CreatedAt).TotalDays <= 45 && t.CreatedAt.Year == m.Year && t.CreatedAt.Month == m.Month),
                waiting = tickets.Count(t => t.Status == TicketStatus.Submitted && (utcNow - t.CreatedAt).TotalDays <= 45 && t.CreatedAt.Year == m.Year && t.CreatedAt.Month == m.Month),
                unanswered = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays > 45 && t.CreatedAt.Year == m.Year && t.CreatedAt.Month == m.Month)
            }).ToList();
        }

        if (allowedKeys.Contains(ChartKey.AgingAnalysis))
        {

            var criticalDepts = tickets
                .Where(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays > 180)
                .Select(t => t.AssignedDepartmentId ?? t.DepartmentId)
                .Where(id => id.HasValue)
                .Distinct()
                .Select(id => deptMap.GetValueOrDefault(id!.Value, "Bilinmeyen Birim"))
                .ToList();

            result.Charts[ChartKey.AgingAnalysis.ToJsonKey()] = new AgingDto
            {
                Over45  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 45 && (utcNow - t.CreatedAt).TotalDays < 60),
                Over60  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 60 && (utcNow - t.CreatedAt).TotalDays < 90),
                Over90  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 90 && (utcNow - t.CreatedAt).TotalDays < 180),
                Over180 = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 180),
                CriticalDepartments = criticalDepts
            };
        }

        // ── Chart 2: Satisfaction (Thanks) by Department ─────────────
        if (allowedKeys.Contains(ChartKey.SatisfactionByDepartment))
        {
            result.Charts[ChartKey.SatisfactionByDepartment.ToJsonKey()] = tickets
                .Where(t => t.Category == TicketCategory.Thanks && (t.AssignedDepartmentId ?? t.DepartmentId).HasValue)
                .GroupBy(t => (t.AssignedDepartmentId ?? t.DepartmentId)!.Value)
                .Select(g => new DepartmentCountDto
                {
                    Name = deptMap.GetValueOrDefault(g.Key, "Unknown"),
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList();
        }

        // ── Chart 3: Complaint by Department ────────────────────────
        if (allowedKeys.Contains(ChartKey.ComplaintByDepartment))
        {
            result.Charts[ChartKey.ComplaintByDepartment.ToJsonKey()] = tickets
                .Where(t => t.Category == TicketCategory.Complaint && (t.AssignedDepartmentId ?? t.DepartmentId).HasValue)
                .GroupBy(t => (t.AssignedDepartmentId ?? t.DepartmentId)!.Value)
                .Select(g => new DepartmentCountDto
                {
                    Name = deptMap.GetValueOrDefault(g.Key, "Unknown"),
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList();
        }

        // ── Chart 4: Staff Type Distribution (İdari/Akademik/İşçi/Öğrenci/Diğer) ──
        if (allowedKeys.Contains(ChartKey.StaffTypeDistribution))
        {
            // Resolve PersonTypes for all unique creators in the set
            var creatorIds = tickets.Where(t => t.CreatorId.HasValue).Select(t => t.CreatorId!.Value).Distinct().ToList();
            var creators = await _userService.GetByIdsAsync(creatorIds);

            var typeLabels = new Dictionary<PersonType, string>
            {
                { PersonType.Academic, "Akademik" },
                { PersonType.Administrative, "İdari" },
                { PersonType.Worker, "İşçi" },
                { PersonType.Student, "Öğrenci" },
                { PersonType.Other, "Diğer" }
            };

            result.Charts[ChartKey.StaffTypeDistribution.ToJsonKey()] = tickets
                .GroupBy(t => t.CreatorId.HasValue && creators.TryGetValue(t.CreatorId.Value, out var c) 
                    ? c.PersonType 
                    : PersonType.Other)
                .Select(g => new StaffTypeCategoryDto
                {
                    TypeName = typeLabels.GetValueOrDefault(g.Key, "Diğer"),
                    Complaint = g.Count(t => t.Category == TicketCategory.Complaint),
                    Suggestion = g.Count(t => t.Category == TicketCategory.Suggestion),
                    Request = g.Count(t => t.Category == TicketCategory.Request),
                    Thanks = g.Count(t => t.Category == TicketCategory.Thanks),
                    Info = g.Count(t => t.Category == TicketCategory.InfoRequest || t.Category == TicketCategory.Question),
                    Total = g.Count()
                })
                .OrderByDescending(d => d.Total)
                .ToList();
        }

        // ── Chart 5: Average Response Time (Birim bazında ortalama çözüm süresi) ──
        if (allowedKeys.Contains(ChartKey.AverageResponseTime))
        {
            var closedTickets = tickets.Where(t => t.Status == TicketStatus.Closed).ToList();

            var overallAvg = closedTickets.Any()
                ? closedTickets.Average(t => (t.UpdatedAt - t.CreatedAt).TotalDays)
                : 0;

            var byDept = closedTickets
                .Where(t => (t.AssignedDepartmentId ?? t.DepartmentId).HasValue)
                .GroupBy(t => (t.AssignedDepartmentId ?? t.DepartmentId)!.Value)
                .Select(g => new
                {
                    name    = deptMap.GetValueOrDefault(g.Key, "Bilinmeyen"),
                    avgDays = Math.Round(g.Average(t => (t.UpdatedAt - t.CreatedAt).TotalDays), 1),
                    count   = g.Count()
                })
                .OrderBy(x => x.avgDays)
                .ToList();

            var fastest = byDept.FirstOrDefault();
            var slowest = byDept.LastOrDefault();

            result.Charts[ChartKey.AverageResponseTime.ToJsonKey()] = new
            {
                overallAvgDays = Math.Round(overallAvg, 1),
                totalClosed    = closedTickets.Count,
                byDepartment   = byDept,
                fastestDept    = fastest?.name,
                fastestDays    = fastest?.avgDays,
                slowestDept    = slowest?.name,
                slowestDays    = slowest?.avgDays
            };
        }

        // ── Chart 6: Monthly Volume Trend (Son 12 ay talep hacmi + % değişim) ──
        if (allowedKeys.Contains(ChartKey.MonthlyVolumeTrend))
        {
            var last12Months = Enumerable.Range(0, 12)
                .Select(i => utcNow.AddMonths(-i))
                .OrderBy(d => d)
                .Select(d => new { Year = d.Year, Month = d.Month, Label = d.ToString("MMM yy") })
                .ToList();

            // Precount by year+month for performance
            var countByMonth = tickets
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .ToDictionary(g => g.Key, g => g.Count());

            var monthlyData = last12Months.Select((m, idx) =>
            {
                var key = new { m.Year, m.Month };
                var count = countByMonth.GetValueOrDefault(key, 0);
                double? changePercent = null;
                if (idx > 0)
                {
                    var prev = last12Months[idx - 1];
                    var prevKey = new { prev.Year, prev.Month };
                    var prevCount = countByMonth.GetValueOrDefault(prevKey, 0);
                    changePercent = prevCount > 0
                        ? Math.Round((double)(count - prevCount) / prevCount * 100, 1)
                        : (double?)null;
                }
                return new { month = m.Label, count, changePercent };
            }).ToList();

            // Peak month
            var peak = monthlyData.OrderByDescending(x => x.count).FirstOrDefault();
            // Latest month vs previous month
            var lastCount = monthlyData.LastOrDefault()?.count ?? 0;
            var prevCount2 = monthlyData.Count >= 2 ? monthlyData[^2].count : 0;
            var latestChange = prevCount2 > 0
                ? Math.Round((double)(lastCount - prevCount2) / prevCount2 * 100, 1)
                : (double?)null;

            result.Charts[ChartKey.MonthlyVolumeTrend.ToJsonKey()] = new
            {
                months       = monthlyData,
                peakMonth    = peak?.month,
                peakCount    = peak?.count,
                latestCount  = lastCount,
                latestChange
            };
        }

        // ── Chart 7: Priority × Status Matrix (Öncelik × Durum çapraz tablosu) ──
        if (allowedKeys.Contains(ChartKey.PriorityStatusMatrix))
        {
            static string GetPriLabel(TicketPriority p) => p switch
            {
                TicketPriority.Critical  => "Kritik",
                TicketPriority.Important => "Önemli",
                TicketPriority.Low       => "Düşük",
                _                        => "Normal"
            };
            static string GetStatusLabel(TicketStatus s) => s switch
            {
                TicketStatus.Submitted        => "Yeni",
                TicketStatus.Reviewing        => "İnceleniyor",
                TicketStatus.WaitingDepartment => "Birimde",
                TicketStatus.Answered         => "Cevaplandı",
                TicketStatus.Closed           => "Kapatıldı",
                _                             => "Bilinmiyor"
            };

            var priorities = new[] {
                TicketPriority.Critical, TicketPriority.Important,
                TicketPriority.Normal,   TicketPriority.Low
            };
            var statuses = new[] {
                TicketStatus.Submitted, TicketStatus.Reviewing,
                TicketStatus.WaitingDepartment, TicketStatus.Answered, TicketStatus.Closed
            };

            var matrix = priorities.Select(p => new
            {
                priority = GetPriLabel(p),
                priorityRaw = (int)p,
                total = tickets.Count(t => t.Priority == p),
                byStatus = statuses.Select(s => new
                {
                    status = GetStatusLabel(s),
                    count  = tickets.Count(t => t.Priority == p && t.Status == s)
                }).ToList()
            }).ToList();

            var criticalOpen = tickets.Count(t =>
                t.Priority == TicketPriority.Critical && t.Status != TicketStatus.Closed);
            var importantOpen = tickets.Count(t =>
                t.Priority == TicketPriority.Important && t.Status != TicketStatus.Closed);

            result.Charts[ChartKey.PriorityStatusMatrix.ToJsonKey()] = new
            {
                matrix,
                statuses     = statuses.Select(GetStatusLabel).ToList(),
                criticalOpen,
                importantOpen,
                totalOpen    = tickets.Count(t => t.Status != TicketStatus.Closed)
            };
        }

        return ServiceResult<PermissionFilteredChartsDto>.Success(result);
    }
}
