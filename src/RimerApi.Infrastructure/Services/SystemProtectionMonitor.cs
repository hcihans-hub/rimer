using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// A background guardian that polls system pressure (Hangfire queue depth) 
/// completely out-of-band. Ensures API requests never block checking queue sizes.
/// Exposes O(1) static volatile properties for instantaneous load shedding routing.
/// </summary>
public class SystemProtectionMonitor : BackgroundService
{
    private readonly ILogger<SystemProtectionMonitor> _logger;

    // Safety: O(1) static volatile fields for lightning-fast reads by middleware
    private static volatile int _currentQueueDepth;
    private static volatile bool _shedLowPriority;
    private static volatile bool _shedNormalPriority;
    private static volatile bool _protectionModeActive;
    private static DateTime _protectionStartTime;

    // Exported State
    public static int CurrentQueueDepth => _currentQueueDepth;
    public static bool ShedLowPriority => _shedLowPriority;
    public static bool ShedNormalPriority => _shedNormalPriority;
    public static bool ProtectionModeActive => _protectionModeActive;

    // Metrics
    private static readonly Meter _meter = new("RimerApi.SystemProtection", "1.0.0");
    private static readonly ObservableGauge<int> _protectionModeGauge = _meter.CreateObservableGauge(
        "system_protection_mode_active",
        () => _protectionModeActive ? 1 : 0,
        description: "1 if the system is currently under extreme pressure and operating in fail-open circuit breaker mode.");

    public SystemProtectionMonitor(ILogger<SystemProtectionMonitor> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SystemProtectionMonitor started. Monitoring Hangfire queue metrics asynchronously.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Safe out-of-band fetch
                var api = JobStorage.Current?.GetMonitoringApi();
                long queueDepth = api?.EnqueuedCount("default") ?? 0;
                
                // Update internal values safely
                _currentQueueDepth = (int)queueDepth;
                
                // 1. Adaptive Load Shedding Logic
                _shedLowPriority = queueDepth > 20_000;
                _shedNormalPriority = queueDepth > 50_000;

                // 2. Fail-Open Circuit Breaker Mode
                if (queueDepth > 80_000 && !_protectionModeActive)
                {
                    _protectionModeActive = true;
                    _protectionStartTime = DateTime.UtcNow;
                    _logger.LogCritical("SYSTEM PROTECTION MODE INITIATED: Queue depth ({QueueDepth}) breached 80K. Disabling audit logging to preserve API survivability.", queueDepth);
                }
                else if (queueDepth < 60_000 && _protectionModeActive) 
                {
                    // Hysteresis: only recover when it drops significantly below 80k
                    _protectionModeActive = false;
                    _logger.LogInformation("SYSTEM PROTECTION MODE DEACTIVATED: Queue depth resolved to {QueueDepth}. Audit logging restored.", queueDepth);
                }

                if (_protectionModeActive && (DateTime.UtcNow - _protectionStartTime).TotalSeconds > 60)
                {
                    _protectionModeActive = false;
                    _logger.LogWarning("SYSTEM PROTECTION FAIL-SAFE: Protection mode disabled after 60 seconds despite high queue depth.");
                }

                if (_shedLowPriority && !_shedNormalPriority)
                    _logger.LogWarning("Load Shedding Active: Rejecting LOW priority requests. Queue: {QueueDepth}", queueDepth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to monitor Hangfire system health.");
            }

            // Check every 2.5 seconds (Balance between resolution and DB IOPS)
            await Task.Delay(2500, stoppingToken);
        }
    }
}
