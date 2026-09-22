using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RimerApi.API.Models;
using RimerApi.Application.DTOs.Department;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;
using RimerApi.Infrastructure.Data;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly RimerApi.Application.Interfaces.ISystemLogService _systemLogService;
    private readonly ApplicationDbContext _context;

    public DepartmentsController(
        IDepartmentRepository departmentRepository,
        RimerApi.Application.Interfaces.ISystemLogService systemLogService,
        ApplicationDbContext context)
    {
        _departmentRepository = departmentRepository;
        _systemLogService = systemLogService;
        _context = context;
    }

    /// <summary>
    /// Get paginated departments with user and ticket counts.
    /// </summary>
    /// <response code="200">Departments retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<RimerApi.Application.Common.PagedResult<DepartmentResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool onlyActive = true, [FromQuery] string? search = null)
    {
        var (items, totalCount) = await _departmentRepository.GetPagedDepartmentsAsync(page, pageSize, onlyActive, search);

        var deptIds = items.Select(d => d.Id).ToList();

        // Batch load user counts
        var userCounts = await _context.Set<ApplicationUser>()
            .Where(u => u.DepartmentId.HasValue && deptIds.Contains(u.DepartmentId!.Value) && !u.IsDeleted && u.IsActive)
            .GroupBy(u => u.DepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.DeptId, g => g.Count);

        // Batch load active ticket counts
        var ticketCounts = await _context.Tickets
            .Where(t => t.AssignedDepartmentId.HasValue &&
                        deptIds.Contains(t.AssignedDepartmentId!.Value) &&
                        t.Status != TicketStatus.Closed &&
                        !t.IsDeleted)
            .GroupBy(t => t.AssignedDepartmentId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.DeptId, g => g.Count);

        var dtos = items.Select(d => MapToDto(d,
            userCounts.GetValueOrDefault(d.Id, 0),
            ticketCounts.GetValueOrDefault(d.Id, 0)));

        var result = new RimerApi.Application.Common.PagedResult<DepartmentResponseDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<RimerApi.Application.Common.PagedResult<DepartmentResponseDto>>.Ok(result));
    }

    /// <summary>
    /// Get a department by id.
    /// </summary>
    /// <param name="id">Department id.</param>
    /// <response code="200">Department retrieved.</response>
    /// <response code="404">Department not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);

        if (department is null)
            return NotFound(ApiResponse<object>.Fail("Department not found.", 404));

        var userCount = await _context.Set<ApplicationUser>()
            .CountAsync(u => u.DepartmentId == id && !u.IsDeleted && u.IsActive);

        var ticketCount = await _context.Tickets
            .CountAsync(t => t.AssignedDepartmentId == id && t.Status != TicketStatus.Closed && !t.IsDeleted);

        return Ok(ApiResponse<DepartmentResponseDto>.Ok(MapToDto(department, userCount, ticketCount)));
    }

    /// <summary>
    /// Create a new department (Admin only).
    /// </summary>
    /// <param name="dto">Department creation data.</param>
    /// <response code="201">Department created.</response>
    /// <response code="400">Validation error or duplicate name.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(ApiResponse<object>.Fail("Department name is required."));

        if (await _departmentRepository.NameExistsAsync(dto.Name))
            return BadRequest(ApiResponse<object>.Fail("A department with this name already exists."));

        var department = new Department
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedBy = RimerApi.API.Extensions.ClaimsPrincipalExtensions.GetUserId(User).ToString()
        };

        await _departmentRepository.AddAsync(department);
        await _departmentRepository.SaveChangesAsync();

        await _systemLogService.LogAsync(
            RimerApi.API.Extensions.ClaimsPrincipalExtensions.GetUserId(User).ToString(),
            "Create Department",
            "Department",
            department.Id.ToString(),
            $"Birim Oluşturuldu: {department.Name}"
        );

        return StatusCode(201,
            ApiResponse<DepartmentResponseDto>.Created(MapToDto(department, 0, 0), "Department created successfully."));
    }

    /// <summary>
    /// Update a department's name, description, and type (Admin only).
    /// </summary>
    /// <param name="id">Department id.</param>
    /// <param name="dto">Updated department data.</param>
    /// <response code="200">Department updated.</response>
    /// <response code="404">Department not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department is null) return NotFound(ApiResponse<object>.Fail("Department not found.", 404));

        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != department.Name)
        {
            if (await _departmentRepository.NameExistsAsync(dto.Name))
                return BadRequest(ApiResponse<object>.Fail("A department with this name already exists."));

            changes.Add($"Name: {department.Name} → {dto.Name}");
            department.Name = dto.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description != department.Description)
        {
            changes.Add($"Description changed");
            department.Description = dto.Description.Trim();
        }



        _departmentRepository.Update(department);
        await _departmentRepository.SaveChangesAsync();

        if (changes.Any())
        {
            await _systemLogService.LogAsync(
                RimerApi.API.Extensions.ClaimsPrincipalExtensions.GetUserId(User).ToString(),
                "Update Department",
                "Department",
                department.Id.ToString(),
                $"Birim Güncellendi: {department.Name} — {string.Join("; ", changes)}"
            );
        }

        var userCount = await _context.Set<ApplicationUser>()
            .CountAsync(u => u.DepartmentId == id && !u.IsDeleted && u.IsActive);
        var ticketCount = await _context.Tickets
            .CountAsync(t => t.AssignedDepartmentId == id && t.Status != TicketStatus.Closed && !t.IsDeleted);

        return Ok(ApiResponse<DepartmentResponseDto>.Ok(MapToDto(department, userCount, ticketCount), "Department updated successfully."));
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department is null) return NotFound(ApiResponse<object>.Fail("Department not found.", 404));

        department.IsActive = true;
        _departmentRepository.Update(department);
        await _departmentRepository.SaveChangesAsync();

        await _systemLogService.LogAsync(
            RimerApi.API.Extensions.ClaimsPrincipalExtensions.GetUserId(User).ToString(),
            "Activate Department",
            "Department",
            department.Id.ToString(),
            $"Birim Aktifleştirildi: {department.Name}"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Department activated successfully."));
    }

    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department is null) return NotFound(ApiResponse<object>.Fail("Department not found.", 404));

        department.IsActive = false;
        _departmentRepository.Update(department);
        await _departmentRepository.SaveChangesAsync();

        await _systemLogService.LogAsync(
            RimerApi.API.Extensions.ClaimsPrincipalExtensions.GetUserId(User).ToString(),
            "Deactivate Department",
            "Department",
            department.Id.ToString(),
            $"Birim Pasifleştirildi: {department.Name}"
        );

        return Ok(ApiResponse<bool>.Ok(true, "Department deactivated successfully."));
    }

    // ── Private helper ─────────────────────────────────────────────

    private static DepartmentResponseDto MapToDto(Department d, int userCount, int activeTicketCount) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Description = d.Description,
        IsActive = d.IsActive,
        UserCount = userCount,
        ActiveTicketCount = activeTicketCount,
        CreatedAt = d.CreatedAt,
        CreatedBy = d.CreatedBy
    };
}
