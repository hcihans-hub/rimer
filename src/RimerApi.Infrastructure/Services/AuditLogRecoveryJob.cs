using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RimerApi.Infrastructure.Data;
using Hangfire;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Dedicated background job for recovering audit logs stuck in 'Attempted' state.
/// Isolated from the main ingestion path to prevent write contention.
/// </summary>
public class AuditLogRecoveryJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditLogRecoveryJob> _logger;

    private static readonly Meter _meter = new("RimerApi.AuditLogging", "1.0.0");
    private static readonly Counter<int> _recoveryCounter = _meter.CreateCounter<int>(
        "audit_log_recovery_processed_total", 
        description: "Total number of stuck audit logs recovered by the background job.");

    public AuditLogRecoveryJob(ApplicationDbContext dbContext, ILogger<AuditLogRecoveryJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Identifies and marks stuck audit logs as 'Failed'.
    /// Uses batching to ensure short-lived transactions and high performance.
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(10 * 60)] // Prevent job overlap if it takes longer than 10 mins
    public async Task RecoverStuckLogsAsync()
    {
        _logger.LogInformation("Starting AuditLog recovery maintenance job.");
        
        var threshold = DateTime.UtcNow.AddMinutes(-30);
        int totalRecovered = 0;
        bool hasMore = true;

        while (hasMore)
        {
            // Senior Architect: Use TOP (N) with ExecuteUpdateAsync for strict performance and lock safety
            // EF Core 7+ ExecuteUpdateAsync supports simple filters and property updates efficiently
            var batchCount = await _dbContext.AuditLogs
                .Where(a => a.Status == "Attempted" && a.CreatedAt < threshold)
                .OrderBy(a => a.CreatedAt)
                .Take(500)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, "Failed")
                    .SetProperty(b => b.ErrorMessage, "Recovery: Transaction timeout/abandoned."));

            if (batchCount == 0)
            {
                hasMore = false;
            }
            else
            {
                totalRecovered += batchCount;
                _recoveryCounter.Add(batchCount);
                _logger.LogDebug("Recovered batch of {Count} logs.", batchCount);
                
                // Yield to allow other transactions a window if needed
                await Task.Yield();
            }
        }

        if (totalRecovered > 0)
        {
            _logger.LogInformation("AuditLog recovery completed. Total records processed: {Total}", totalRecovered);
        }
    }
}
