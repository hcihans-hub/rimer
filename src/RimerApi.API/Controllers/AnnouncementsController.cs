using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RimerApi.Application.DTOs.Announcement;
using RimerApi.Application.Interfaces;
using RimerApi.API.Models;
using RimerApi.API.Extensions;
using RimerApi.Application.Common;

namespace RimerApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        /// <summary>
        /// Get active announcements for the current user.
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<List<AnnouncementDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveAnnouncements()
        {
            var result = await _announcementService.GetActiveAnnouncementsForUserAsync(User.GetCurrentUser());
            if (result.IsSuccess) return Ok(ApiResponse<List<AnnouncementDto>>.Ok(result.Data!));
            return BadRequest(ApiResponse<List<AnnouncementDto>>.Fail(result.ErrorMessage ?? "Error"));
        }

        /// <summary>
        /// Get all announcements (Admin only).
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<AnnouncementDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAdmin()
        {
            var result = await _announcementService.GetAllAdminAsync();
            if (result.IsSuccess) return Ok(ApiResponse<List<AnnouncementDto>>.Ok(result.Data!));
            return BadRequest(ApiResponse<List<AnnouncementDto>>.Fail(result.ErrorMessage ?? "Error"));
        }

        /// <summary>
        /// Create a new announcement (Admin only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AnnouncementDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
        {
            var result = await _announcementService.CreateAsync(dto, User.GetCurrentUser());
            if (result.IsSuccess) return Ok(ApiResponse<AnnouncementDto>.Ok(result.Data!));
            return BadRequest(ApiResponse<AnnouncementDto>.Fail(result.ErrorMessage ?? "Error"));
        }

        /// <summary>
        /// Update an announcement (Admin only).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AnnouncementDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAnnouncementDto dto)
        {
            var result = await _announcementService.UpdateAsync(id, dto);
            if (result.IsSuccess) return Ok(ApiResponse<AnnouncementDto>.Ok(result.Data!));
            return BadRequest(ApiResponse<AnnouncementDto>.Fail(result.ErrorMessage ?? "Error"));
        }

        /// <summary>
        /// Toggle the active status of an announcement (Admin only).
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _announcementService.ToggleActiveStatusAsync(id);
            if (result.IsSuccess) return Ok(ApiResponse<bool>.Ok(result.Data!));
            return BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage ?? "Error"));
        }

        /// <summary>
        /// Delete an announcement (Admin only).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _announcementService.DeleteAsync(id);
            if (result.IsSuccess) return Ok(ApiResponse<bool>.Ok(result.Data!));
            return BadRequest(ApiResponse<bool>.Fail(result.ErrorMessage ?? "Error"));
        }
    }
}
