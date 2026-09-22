using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Dashboard;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Provides aggregated statistics for the admin dashboard.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get a full snapshot of system statistics for the dashboard.
    /// </summary>
    Task<ServiceResult<DashboardStatsDto>> GetStatsAsync();
}
