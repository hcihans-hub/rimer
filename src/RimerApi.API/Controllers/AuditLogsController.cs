using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RimerApi.API.Models;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Application.Interfaces;
using System.Threading.Tasks;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Retrieves a paginated list of global audit logs.
    /// Supports filtering by TableName, RecordId, UserId, and Date ranges.
    /// </summary>
    /// <param name="filter">Filter criteria including pagination parameters.</param>
    /// <returns>Paginated audit logs with deserialized json values.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogFilterDto filter)
    {
        var result = await _auditLogService.GetListAsync(filter);
        
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.ErrorMessage!, result.ErrorCode ?? 400));

        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result.Data!));
    }
}
