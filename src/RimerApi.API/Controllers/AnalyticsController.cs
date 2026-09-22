using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Extensions;
using RimerApi.Application.DTOs.Analytics;
using RimerApi.Application.Interfaces;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IChartPermissionService _chartService;

    public AnalyticsController(IChartPermissionService chartService)
    {
        _chartService = chartService;
    }

    /// <summary>
    /// Get chart data filtered by the authenticated user's permissions.
    /// Returns ONLY charts the user has been granted access to.
    /// </summary>
    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _chartService.GetPermittedChartsAsync(startDate, endDate, User.GetCurrentUser());

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all chart permissions for all users. Admin only.
    /// </summary>
    [HttpGet("admin/chart-permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var result = await _chartService.GetAllPermissionsAsync(User.GetCurrentUser());

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get chart permissions for a specific user. Admin only.
    /// </summary>
    [HttpGet("admin/chart-permissions/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPermissions(Guid userId)
    {
        var result = await _chartService.GetPermissionsAsync(userId, User.GetCurrentUser());

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Assign chart permissions to a user. Admin only.
    /// </summary>
    [HttpPost("admin/chart-permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignPermissions([FromBody] AssignChartPermissionsDto dto)
    {
        var result = await _chartService.AssignPermissionsAsync(dto, User.GetCurrentUser());

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
}
