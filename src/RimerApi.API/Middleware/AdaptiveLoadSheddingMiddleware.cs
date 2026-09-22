using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Attributes;
using RimerApi.Application.Enums;
using RimerApi.Infrastructure.Services;

namespace RimerApi.API.Middleware;

/// <summary>
/// A high-performance load shedding middleware.
/// Rejects lower priority requests with HTTP 429 when the system is under pressure.
/// Ultra-hardened for extreme production concurrency.
/// </summary>
public class AdaptiveLoadSheddingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdaptiveLoadSheddingMiddleware> _logger;
    private static long _logCounter = 0;

    // 6. GLOBAL KILL SWITCH: Instantly returns 503 for all traffic when true
    public static volatile bool GlobalKillSwitchActive = false;

    // 5. PROTECT METRICS: Counters directly injected
    private static readonly Meter _protectionMeter = new("RimerApi.SystemProtection", "1.0.0");
    private static readonly Counter<int> _suppressedCounter = _protectionMeter.CreateCounter<int>("audit_log_suppressed_total");

    // 1. PATH SEGMENTS: Zero-allocation PathString definitions for security and performance
    private static readonly PathString _authPrefix = new PathString("/api/auth");
    private static readonly PathString _healthPrefix = new PathString("/health");
    private static readonly PathString _metricsPrefix = new PathString("/metrics");

    // 8. IP RATE LIMITER: Zero allocation token bucket for critical route protection
    private static readonly RimerApi.API.Providers.LightweightIpRateLimiter _ipLimiter = new(capacity: 20, refillRatePerSecond: 5);

    public AdaptiveLoadSheddingMiddleware(RequestDelegate next, ILogger<AdaptiveLoadSheddingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 6. GLOBAL KILL SWITCH
        if (GlobalKillSwitchActive)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            // 3. PREVENT HEADER DUPLICATION: Indexer used
            context.Response.Headers["Retry-After"] = "60"; 
            context.Response.Headers["Cache-Control"] = "no-store, no-cache";
            return;
        }

        // FULL THREAD SAFETY: O(1) Quick Check via volatile properties
        bool shedLow = SystemProtectionMonitor.ShedLowPriority;
        bool shedNormal = SystemProtectionMonitor.ShedNormalPriority;
        bool protectionActive = SystemProtectionMonitor.ProtectionModeActive;

        if (!shedLow && !shedNormal && !protectionActive)
        {
            await _next(context);
            return;
        }

        // KEEP ZERO PERFORMANCE OVERHEAD
        var endpoint = context.GetEndpoint();
        var priorityAttribute = endpoint?.Metadata.GetMetadata<RequestPriorityAttribute>();
        
        var priority = priorityAttribute?.Priority ?? RequestPriority.Low;

        // 8. IP RATE LIMITER: Protect against queue bombing array/abuse
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!_ipLimiter.IsAllowed(ip))
        {
            // Drop spammers instantly, regardless of priority or whitelist
            _suppressedCounter.Add(1, 
                new KeyValuePair<string, object?>("reason", "ip_rate_limit"),
                new KeyValuePair<string, object?>("priority", GetPriorityLabel(priority)));

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "5"; 
            context.Response.Headers["Cache-Control"] = "no-store, no-cache";
            return;
        }

        // 1 & 2. NORMALIZE & SECURE: Safe Segment Evaluation
        if (priority == RequestPriority.Critical)
        {
            var path = context.Request.Path;
            bool isWhitelisted = path.StartsWithSegments(_authPrefix, StringComparison.OrdinalIgnoreCase) ||
                                 path.StartsWithSegments(_healthPrefix, StringComparison.OrdinalIgnoreCase) ||
                                 path.StartsWithSegments(_metricsPrefix, StringComparison.OrdinalIgnoreCase);

            if (!isWhitelisted)
            {
                priority = RequestPriority.Normal; // Downgrade
            }
            else
            {
                await _next(context);
                return;
            }
        }

        bool shouldShed = false;
        string errorCode = string.Empty;
        string errorMessage = string.Empty;

        if (protectionActive)
        {
            shouldShed = true;
            errorCode = "SYSTEM_PROTECTION_MODE";
            errorMessage = "System is in circuit-breaker protection mode. API is strictly prioritizing critical transactions only.";
        }
        else if (shedNormal && priority == RequestPriority.Normal)
        {
            shouldShed = true;
            errorCode = "SYSTEM_OVERLOADED";
            errorMessage = "System is overloaded. Normal traffic is temporarily suspended.";
        }
        else if (shedLow && priority == RequestPriority.Low)
        {
            shouldShed = true;
            errorCode = "SYSTEM_BUSY";
            errorMessage = "Background processing queues are full. Low-priority tasks are dropped.";
        }

        if (shouldShed)
        {
            int queueDepth = SystemProtectionMonitor.CurrentQueueDepth;

            // 5. PROTECT METRICS: Fixed string labels instead of ToString() reflection
            _suppressedCounter.Add(1, 
                new KeyValuePair<string, object?>("reason", errorCode),
                new KeyValuePair<string, object?>("priority", GetPriorityLabel(priority)));

            if ((Interlocked.Increment(ref _logCounter) % 100) == 0)
            {
                _logger.LogWarning("Load Shedding sampled log for route {Path} (Priority: {Priority}) due to {ErrorCode}. Queue Depth: {QueueDepth}", 
                    context.Request.Path, priority, errorCode, queueDepth);
            }

            int retryAfterSecs = queueDepth > 80_000 ? 30 :
                                 queueDepth > 50_000 ? 10 :
                                 queueDepth > 20_000 ? 3 : 1;
            
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            // 3. PREVENT HEADER DUPLICATION
            context.Response.Headers["Retry-After"] = retryAfterSecs.ToString();
            context.Response.Headers["Cache-Control"] = "no-store, no-cache";
            context.Response.ContentType = "application/json";

            // 4. REDUCE ALLOCATION: Use struct definition
            var responseObj = new LoadSheddingResponse(
                errorCode, 
                errorMessage, 
                retryAfterSecs * 1000, 
                GetPriorityLabel(priority), 
                Activity.Current?.Id ?? context.TraceIdentifier);

            await JsonSerializer.SerializeAsync(context.Response.Body, responseObj, cancellationToken: context.RequestAborted);
            return; 
        }

        await _next(context);
    }

    private static string GetPriorityLabel(RequestPriority priority)
    {
        return priority switch
        {
            RequestPriority.Critical => "Critical",
            RequestPriority.Normal => "Normal",
            RequestPriority.Low => "Low",
            _ => "Unknown"
        };
    }

    // 4. REDUCE ALLOCATION: Allocation-free struct records for serialization
    private readonly struct LoadSheddingResponse
    {
        public ErrorDetails Error { get; }
        
        public LoadSheddingResponse(string code, string message, int retryAfterMs, string priorityRejected, string traceId)
        {
            Error = new ErrorDetails(code, message, retryAfterMs, priorityRejected, traceId);
        }
    }

    private readonly struct ErrorDetails
    {
        public string Code { get; }
        public string Message { get; }
        public int RetryAfterMs { get; }
        public string PriorityRejected { get; }
        public string TraceId { get; }

        public ErrorDetails(string code, string message, int retryAfterMs, string priorityRejected, string traceId)
        {
            Code = code;
            Message = message;
            RetryAfterMs = retryAfterMs;
            PriorityRejected = priorityRejected;
            TraceId = traceId;
        }
    }
}
