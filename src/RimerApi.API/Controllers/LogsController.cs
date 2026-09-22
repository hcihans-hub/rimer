using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Models;
using RimerApi.Application.Common;
using RimerApi.Application.Interfaces;
using System.Threading.Tasks;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly ISystemLogService _systemLogService;

    public LogsController(ISystemLogService systemLogService)
    {
        _systemLogService = systemLogService;
    }

    /// <summary>
    /// Get paginated system logs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SystemLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? userName = null,
        [FromQuery] string? details = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _systemLogService.GetLogsAsync(page, pageSize, action, userName, details, startDate, endDate);
        return Ok(ApiResponse<PagedResult<SystemLogDto>>.Ok(result.Data!));
    }
}
