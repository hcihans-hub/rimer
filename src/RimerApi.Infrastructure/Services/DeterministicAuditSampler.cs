using System;
using RimerApi.Application.DTOs.AuditLog;
using RimerApi.Application.Enums;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// Hash-based deterministic sampler for audit logs under heavy queue pressure.
/// Protects against exponential DB growth/IOPS exhaustion without fully disabling auditing.
/// </summary>
public static class DeterministicAuditSampler
{
    public static bool ShouldSample(AuditLogMessageDto dto, int currentQueueDepth)
    {
        if (dto.CorrelationId == Guid.Empty)
            return true;

        if (dto.Priority == RequestPriority.Critical)
            return true;

        int thresholdPercent = 100;

        if (dto.Priority == RequestPriority.Low)
        {
            thresholdPercent = 10;
        }
        else if (dto.Priority == RequestPriority.Normal)
        {
            if (currentQueueDepth > 80_000)
                return false; 
            
            if (currentQueueDepth > 50_000)
                thresholdPercent = 10; 
            else if (currentQueueDepth > 20_000)
                thresholdPercent = 50; 
        }

        if (thresholdPercent == 100) return true;
        if (thresholdPercent == 0) return false;
        
        // FIX 1: Stable deterministic hash (FNV-1a - zero allocation)
        Span<byte> guidBytes = stackalloc byte[16];
        if (!dto.CorrelationId.TryWriteBytes(guidBytes)) return true; // FAIL-OPEN (log kaybetmemek için)
        
        uint hash = 2166136261;
        for (int i = 0; i < 16; i++)
        {
            hash ^= guidBytes[i];
            hash *= 16777619;
        }
        
        int bucket = (int)(hash % 100);
        return bucket < thresholdPercent;
    }
}
