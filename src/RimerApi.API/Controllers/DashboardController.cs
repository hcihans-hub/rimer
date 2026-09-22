using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Models;
using RimerApi.Application.DTOs.Dashboard;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

/// <summary>
/// Admin dashboard statistics endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get a full statistics snapshot for the admin dashboard.
    /// Includes ticket overview, by-category, by-department breakdowns,
    /// 14-day trend, and recent activity.
    /// </summary>
    /// <response code="200">Statistics retrieved successfully.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        var result = await _dashboardService.GetStatsAsync();

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 500,
                ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 500));

        return Ok(ApiResponse<DashboardStatsDto>.Ok(result.Data!));
    }
}
