using System;
using Microsoft.AspNetCore.Mvc;
using RimerApi.Application.Interfaces;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Infrastructure.Services;
using RimerApi.Application.Enums;
using RimerApi.Application.Attributes;

namespace RimerApi.API.Controllers;

[ApiController]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogQueueService _auditLogQueueService;

    public AuditController(IAuditLogQueueService auditLogQueueService)
    {
        _auditLogQueueService = auditLogQueueService;
    }

    [HttpPost]
    [RequestPriorityAttribute(RequestPriority.Normal)]
    public IActionResult CreateAuditLog([FromBody] AuditRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        bool isLowPriority = request.Priority?.Equals("Low", StringComparison.OrdinalIgnoreCase) == true;

        if (SystemProtectionMonitor.ProtectionModeActive)
        {
            return StatusCode(429, new { error = "System overloaded" });
        }

        if (isLowPriority && SystemProtectionMonitor.ShedLowPriority)
        {
            return StatusCode(429, new { error = "System overloaded. Low priority shed." });
        }

        var logMessage = new AuditLogMessageDto
        {
            CorrelationId = Guid.NewGuid(),
            TableName = "ManualAudit",
            Action = request.Message,
            Priority = isLowPriority ? RequestPriority.Low : RequestPriority.Normal,
            OccurredAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        _auditLogQueueService.EnqueueAuditLogs(new[] { logMessage });

        return Ok(new { status = "Accepted" });
    }
}

public class AuditRequest
{
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}
