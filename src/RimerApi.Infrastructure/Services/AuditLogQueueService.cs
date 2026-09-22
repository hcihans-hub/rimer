using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace RimerApi.Infrastructure.Services;

public class AuditLogQueueService : IAuditLogQueueService
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<AuditLogQueueService> _logger;

    // Safe low-cardinality metrics for OTel/Prometheus
    private static readonly Meter _protectionMeter = new("RimerApi.SystemProtection", "1.0.0");
    private static readonly Counter<int> _droppedCounter = _protectionMeter.CreateCounter<int>("audit_log_dropped_total");
    private static readonly Counter<int> _sampledCounter = _protectionMeter.CreateCounter<int>("audit_log_sampled_total");

    public AuditLogQueueService(IBackgroundJobService backgroundJobService, ILogger<AuditLogQueueService> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    // 3) ENUM STRING ALLOCATION: Zero allocation mapping
    private static string GetPriorityLabel(RimerApi.Application.Enums.RequestPriority priority)
    {
        return priority switch
        {
            RimerApi.Application.Enums.RequestPriority.Critical => "Critical",
            RimerApi.Application.Enums.RequestPriority.Normal => "Normal",
            RimerApi.Application.Enums.RequestPriority.Low => "Low",
            _ => "Unknown"
        };
    }

    public void EnqueueAuditLogs(IEnumerable<AuditLogMessageDto> logs)
    {
        if (logs == null) return;

        int queueDepth = SystemProtectionMonitor.CurrentQueueDepth;
        const int HARD_LIMIT = 100_000;

        if (queueDepth >= HARD_LIMIT)
        {
            bool hasExactCount = logs.TryGetNonEnumeratedCount(out int count);
            int droppedCount = hasExactCount ? (count > 1000 ? 1000 : count) : 100;

            _logger.LogCritical("HARD LIMIT BREACHED: Audit queue depth ({QueueDepth}) >= 100k. Dropping batch pre-emptively.", queueDepth);

            _droppedCounter.Add(droppedCount,
                new KeyValuePair<string, object?>("reason", "hard_limit"),
                new KeyValuePair<string, object?>("estimated", hasExactCount ? "false" : "true"));

            return;
        }

        // SAFE ENUMERATION (NO MULTI ENUM / NO INFINITE STREAM)
        IEnumerable<AuditLogMessageDto> safeLogs =
            logs is ICollection<AuditLogMessageDto> || logs is IReadOnlyCollection<AuditLogMessageDto>
            ? logs
            : logs.Take(10_000);

        const int BATCH_SIZE = 500;
        const int CIRCUIT_BREAKER_LIMIT = 80_000;
        const int LOW_DROPPING_LIMIT = 20_000;

        var chunk = new List<AuditLogMessageDto>(BATCH_SIZE);

        int sampledOutCount = 0;
        int protectionModeDropCount = 0;
        int ingestionCapDropCount = 0;

        int processedCount = 0;

        foreach (var log in safeLogs)
        {
            processedCount++;

            // HARD INGESTION CAP (TRUE CAP → BREAK)
            if (processedCount > 10_000)
            {
                ingestionCapDropCount++;
                break;
            }

            // QUEUE DEPTH REFRESH (CORRECT TIMING)
            if ((processedCount & 255) == 0)
            {
                queueDepth = SystemProtectionMonitor.CurrentQueueDepth;
            }

            bool forceDrop = false;

            if (queueDepth >= CIRCUIT_BREAKER_LIMIT || SystemProtectionMonitor.ProtectionModeActive)
            {
                if (log.Priority != RimerApi.Application.Enums.RequestPriority.Critical)
                    forceDrop = true;
            }
            else if (queueDepth >= LOW_DROPPING_LIMIT &&
                    log.Priority == RimerApi.Application.Enums.RequestPriority.Low)
            {
                forceDrop = true;
            }

            if (forceDrop)
            {
                protectionModeDropCount++;
                continue;
            }

            if (DeterministicAuditSampler.ShouldSample(log, queueDepth))
            {
                chunk.Add(log);
            }
            else
            {
                sampledOutCount++;
            }

            if (chunk.Count >= BATCH_SIZE)
            {
                // ⚠️ CRITICAL CONTRACT: Do NOT mutate after enqueue
                IReadOnlyList<AuditLogMessageDto> payload = chunk;

                _backgroundJobService.Enqueue<AuditLogQueueWorker>(
                    worker => worker.ProcessBatchAsync(payload));

                chunk = new List<AuditLogMessageDto>(BATCH_SIZE);
            }
        }

        if (chunk.Count > 0)
        {
            // ⚠️ CRITICAL CONTRACT
            IReadOnlyList<AuditLogMessageDto> finalPayload = chunk;

            _backgroundJobService.Enqueue<AuditLogQueueWorker>(
                worker => worker.ProcessBatchAsync(finalPayload));
        }

        if (ingestionCapDropCount > 0)
        {
            _droppedCounter.Add(ingestionCapDropCount,
                new KeyValuePair<string, object?>("reason", "ingestion_cap"),
                new KeyValuePair<string, object?>("estimated", "false"));
        }

        if (sampledOutCount > 0)
        {
            _sampledCounter.Add(sampledOutCount,
                new KeyValuePair<string, object?>("reason", "sampled_out"));
        }

        if (protectionModeDropCount > 0)
        {
            _droppedCounter.Add(protectionModeDropCount,
                new KeyValuePair<string, object?>("reason", "protection_mode"));
        }
    }
}