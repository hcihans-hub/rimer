using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Implements cold-storage archival of audit logs compressing to GZip locally. 
/// In production, this can be swapped with Amazon S3 or Azure Blob implementation.
/// </summary>
public class LocalGzipAuditArchiveProvider : IAuditArchiveProvider
{
    private readonly ILogger<LocalGzipAuditArchiveProvider> _logger;
    private readonly string _archivePath = Path.Combine(Directory.GetCurrentDirectory(), "AuditArchives");

    public LocalGzipAuditArchiveProvider(ILogger<LocalGzipAuditArchiveProvider> logger)
    {
        _logger = logger;
        if (!Directory.Exists(_archivePath))
        {
            Directory.CreateDirectory(_archivePath);
        }
    }

    public async Task<string> ArchiveBatchAsync(IEnumerable<AuditLog> logs, string batchCorrelationId)
    {
        var fileName = $"audit_batch_{batchCorrelationId}.json.gz";
        var fullPath = Path.Combine(_archivePath, fileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        
        var options = new JsonSerializerOptions { WriteIndented = false };
        await JsonSerializer.SerializeAsync(gzipStream, logs, options);

        _logger.LogInformation("Successfully compressed and archived audit log batch to {Path}", fullPath);
        return fullPath;
    }
}
