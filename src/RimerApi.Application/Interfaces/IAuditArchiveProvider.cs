using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimerApi.Domain.Entities;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Proivdes an abstraction for archiving generic audit logs securely.
/// </summary>
public interface IAuditArchiveProvider
{
    /// <summary>
    /// Archives a batch of audit logs. Returns the absolute path, URL or blob identifier of the successful archive.
    /// </summary>
    Task<string> ArchiveBatchAsync(IEnumerable<AuditLog> logs, string batchCorrelationId);
}
