using Microsoft.AspNetCore.Mvc;
using RimerApi.Application.DTOs.Public;
using RimerApi.Application.Services;

namespace RimerApi.API.Controllers;

/// <summary>
/// Public endpoints that do NOT require authentication.
/// Handles external ticket submissions and tracking.
/// </summary>
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly PublicTicketService _publicService;

    public PublicController(PublicTicketService publicService)
    {
        _publicService = publicService;
    }

    /// <summary>
    /// Submit a public ticket without authentication.
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] PublicApplyDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _publicService.CreatePublicTicketAsync(dto);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Track a public ticket by tracking code + email verification.
    /// </summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] PublicTrackRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _publicService.TrackTicketAsync(dto);

        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Gets a list of recent ticket events for the public ticker.
    /// </summary>
    [HttpGet("ticker")]
    public async Task<IActionResult> GetTicker()
    {
        var result = await _publicService.GetPublicTickerAsync();
        if (!result.IsSuccess)
            return StatusCode(result.ErrorCode ?? 400, new { success = false, message = result.ErrorMessage });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// List all departments for the public application form.
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var depts = await _publicService.GetDepartmentsAsync();
        return Ok(new { success = true, data = depts });
    }
}
