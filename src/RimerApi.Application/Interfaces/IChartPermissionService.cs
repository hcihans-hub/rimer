using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Analytics;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Service for managing chart permissions and fetching permission-filtered chart data.
/// </summary>
public interface IChartPermissionService
{
    /// <summary>Get all chart permissions for all users (Admin only).</summary>
    Task<ServiceResult<List<UserChartPermissionDto>>> GetAllPermissionsAsync(CurrentUser currentUser);

    /// <summary>Get permitted chart keys for a user.</summary>
    Task<ServiceResult<List<string>>> GetPermissionsAsync(Guid userId, CurrentUser currentUser);

    /// <summary>Assign chart permissions to a user. Admin only.</summary>
    Task<ServiceResult<List<string>>> AssignPermissionsAsync(AssignChartPermissionsDto dto, CurrentUser currentUser);

    /// <summary>Get chart data filtered by the authenticated user's permissions.</summary>
    Task<ServiceResult<PermissionFilteredChartsDto>> GetPermittedChartsAsync(DateTime? startDate, DateTime? endDate, CurrentUser currentUser);
}
