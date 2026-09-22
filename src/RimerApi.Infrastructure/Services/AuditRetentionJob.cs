using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;
using RimerApi.Infrastructure.Data;

namespace RimerApi.Infrastructure.Services;

public class AuditRetentionJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditArchiveProvider _archiveProvider;
    private readonly ILogger<AuditRetentionJob> _logger;

    public AuditRetentionJob(
        ApplicationDbContext dbContext,
        IAuditArchiveProvider archiveProvider,
        ILogger<AuditRetentionJob> logger)
    {
        _dbContext = dbContext;
        _archiveProvider = archiveProvider;
        _logger = logger;
    }

    /// <summary>
    /// Intended to be run periodically via Hangfire (e.g., daily).
    /// Archives records older than X days, marks them IsArchived, and permanently deletes records older than Y days.
    /// </summary>
    public async Task ProcessRetentionAsync(int archiveDays = 7, int deleteDays = 30)
    {
        var now = DateTime.UtcNow;
        var archiveThreshold = now.AddDays(-archiveDays);
        var deleteThreshold = now.AddDays(-deleteDays);

        // 1. Permanently delete successfully archived records that have crossed the strict death threshold
        var deletedCount = await _dbContext.AuditLogs
            .Where(a => a.IsArchived == true && a.CreatedAt < deleteThreshold)
            .ExecuteDeleteAsync();

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {DeletedCount} expired and safely archived audit logs.", deletedCount);
        }

        // 2. Archive records that crossed the soft threshold, batching by 10,000 for compression
        int batchSize = 10000;
        bool hasMore = true;

        while (hasMore)
        {
            var targetLogs = await _dbContext.AuditLogs
                .Where(a => a.IsArchived == false && a.CreatedAt < archiveThreshold)
                .OrderBy(a => a.CreatedAt)
                .Take(batchSize)
                .ToListAsync();

            if (!targetLogs.Any())
            {
                hasMore = false;
                continue;
            }

            var batchCorrelation = Guid.NewGuid().ToString("N");
            
            try 
            {
                // A) Write to cold storage
                var archiveUri = await _archiveProvider.ArchiveBatchAsync(targetLogs, batchCorrelation);
                var logIds = targetLogs.Select(l => l.Id).ToList();

                // B) Update Database source of truth atomically
                await _dbContext.AuditLogs
                    .Where(a => logIds.Contains(a.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsArchived, true)
                        .SetProperty(x => x.ArchivedAt, DateTime.UtcNow)
                    );

                _logger.LogInformation("Successfully archived {Count} logs to {Uri}", targetLogs.Count, archiveUri);
            }
            catch (Exception ex)
            {
                // Strict compliance rule: Do NOT delete or mark IsArchived if upload fails
                _logger.LogError(ex, "Failed to archive audit log batch {BatchId}. Execution halted to prevent data loss.", batchCorrelation);
                throw; // Rethrow to let Hangfire know the job failed
            }
        }
    }
}
