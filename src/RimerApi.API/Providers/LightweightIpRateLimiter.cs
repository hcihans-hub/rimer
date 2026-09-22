using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RimerApi.API.Providers;

/// <summary>
/// A zero-allocation, lock-free token bucket implementation for IP tracking.
/// Protected against Unbounded memory limit Botnet attacks.
/// </summary>
public sealed class LightweightIpRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new(StringComparer.Ordinal);
    private readonly int _capacity;
    private readonly int _refillRatePerSecond;
    
    // FIX 3: MEMORY SAFETY & CAP
    private readonly int _maxEntries;
    private readonly long _ttlMs;
    private int _currentCount;

    public LightweightIpRateLimiter(int capacity = 10, int refillRatePerSecond = 2, int maxEntries = 100_000, int ttlMinutes = 10)
    {
        _capacity = capacity;
        _refillRatePerSecond = refillRatePerSecond;
        _maxEntries = maxEntries;
        _ttlMs = (long)TimeSpan.FromMinutes(ttlMinutes).TotalMilliseconds;
    }

    public bool IsAllowed(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return true;

        if (_buckets.TryGetValue(ip, out var bucket))
        {
            bucket.UpdateLastSeen();
            return bucket.Consume(_capacity, _refillRatePerSecond);
        }

        // PRE-EMPTIVE CLEANUP for botnets
        if (Volatile.Read(ref _currentCount) >= _maxEntries)
        {
            bool cleaned = IncrementalCleanup();
            if (Volatile.Read(ref _currentCount) >= _maxEntries)
            {
                if (!cleaned)
                {
                    // 6) RATE LIMITER DICTIONARY PRESSURE: Pseudo-random eviction if completely stalled
                    foreach (var key in _buckets.Keys)
                    {
                        if (_buckets.TryRemove(key, out _))
                        {
                            Interlocked.Decrement(ref _currentCount);
                            break;
                        }
                    }
                }
                else
                {
                    return false; // Absolute lockdown: deny new IPs while saturated
                }
            }
        }

        bucket = new TokenBucket(_capacity);
        if (_buckets.TryAdd(ip, bucket))
        {
            Interlocked.Increment(ref _currentCount);
        }
        else
        {
            bucket = _buckets[ip]; // Handle thread race
        }

        return bucket.Consume(_capacity, _refillRatePerSecond);
    }

    private int _isCleaning;

    private bool IncrementalCleanup()
    {
        // 4) IP LIMITER EDGE CASE: Bounded, non-blocking lazy cleanup
        if (Interlocked.CompareExchange(ref _isCleaning, 1, 0) != 0) return false;

        bool removedAny = false;
        try
        {
            long now = Environment.TickCount64;
            int scanned = 0;

            foreach (var kvp in _buckets)
            {
                if (scanned++ >= 500) break; // Bounded processing loop

                if (unchecked(now - Volatile.Read(ref kvp.Value.LastSeen)) > _ttlMs)
                {
                    if (_buckets.TryRemove(kvp.Key, out _))
                    {
                        Interlocked.Decrement(ref _currentCount);
                        removedAny = true;
                    }
                }
            }
        }
        finally
        {
            Volatile.Write(ref _isCleaning, 0);
        }
        return removedAny;
    }

    private sealed class TokenBucket
    {
        public long LastSeen;
        private long _lastRefillTicks;
        private int _tokens;

        public TokenBucket(int capacity)
        {
            _tokens = capacity;
            _lastRefillTicks = Environment.TickCount64;
            LastSeen = Environment.TickCount64;
        }

        public void UpdateLastSeen()
        {
            Volatile.Write(ref LastSeen, Environment.TickCount64);
        }

        public bool Consume(int capacity, int refillRatePerSecond)
        {
            long now = Environment.TickCount64;
            long last = Interlocked.Read(ref _lastRefillTicks);
            double secondsElapsed = unchecked(now - last) / 1000.0;

            // 5) TOKEN REFILL DRIFT: Clamp to 5 seconds to prevent burst spikes after long idle states
            if (secondsElapsed > 5.0) secondsElapsed = 5.0;

            if (secondsElapsed >= 1.0)
            {
                int tokensToAdd = (int)(secondsElapsed * refillRatePerSecond);
                if (tokensToAdd > 0)
                {
                    if (Interlocked.CompareExchange(ref _lastRefillTicks, now, last) == last)
                    {
                        int currentTokens = Volatile.Read(ref _tokens);
                        int newTokens = Math.Min(capacity, currentTokens + tokensToAdd);
                        Interlocked.Exchange(ref _tokens, newTokens);
                    }
                }
            }

            // FIX 4: TOKEN BUCKET RACE CONDITION - CAS Loop
            int current;
            do
            {
                current = Volatile.Read(ref _tokens);
                if (current <= 0) return false;
            } while (Interlocked.CompareExchange(ref _tokens, current - 1, current) != current);

            return true;
        }
    }
}
