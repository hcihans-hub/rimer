using System.Collections.Generic;
using RimerApi.Application.DTOs.AuditLog;

namespace RimerApi.Application.Interfaces;

public interface IAuditLogQueueService
{
    void EnqueueAuditLogs(IEnumerable<AuditLogMessageDto> logs);
}
