# Rimer API: Chaos Engineering & Failure Simulation Plan

**Date:** 02 May 2026
**Author:** Hasan Cihan Şenküçük Software Developer 
**Classification:** Technical / Infrastructure
**System Profile:** Moderate load (1K–10K requests/day), burst capable.  
**Philosophy:** Pragmatic Chaos Engineering. No over-engineering; utilizing native tools, PowerShell, and application hooks.

---

## 1. GENERAL AUTOMATION & SAFETY STRATEGY

### 1.1 Safe Simulation Hooks
To ensure chaos tests do not bleed into production, simulations are triggered via:
1. **Infrastructure Scripts:** PowerShell/Bash scripts targeting network/OS primitives.
2. **Chaos Endpoints (Feature Flagged):** A specific controller (e.g., `/api/chaos/`) only active when `ASPNETCORE_ENVIRONMENT=Staging` or `ChaosEngineering:Enabled=true` in `appsettings.json`.

### 1.2 Guard Rails
*   **Timeouts:** All chaos scripts must have an automatic rollback/timeout (e.g., removing a firewall block after 60 seconds).
*   **No Prod Data:** Never run these tests on the production database.
*   **Targeted Blast Radius:** Start small (affecting one background thread) before taking down external dependencies.

---

## 2. SCENARIO 1: SQL SERVER UNAVAILABLE / SLOW

**Description:** Simulates transient network partitions, maintenance failovers, or massive reporting queries locking the disk.

**Simulation Method (PowerShell):**
```powershell
# Inject a 60-second port block simulating an unresponsive DB
New-NetFirewallRule -DisplayName "Chaos-Block-SQL" -Direction Outbound -RemotePort 1433 -Protocol TCP -Action Block
Start-Sleep -Seconds 60
Remove-NetFirewallRule -DisplayName "Chaos-Block-SQL"
```

**System Behavior Validation:**
*   **Must continue working:** The API must continue accepting requests without hanging.
*   **Allowed to degrade:** API requests requiring synchronous DB reads/writes will fail (returning 500/503). Hangfire jobs will fail and retry.
*   **MUST NEVER happen:** 
    *   Thread pool exhaustion (hanging infinitely on SQL connections).
    *   Application crashing completely resulting in a pod/process death.

**Metric-Based Verification (PromQL):**
```promql
# API Error Rate spikes naturally
rate(http_requests_received_total{code=~"5.."}[1m]) > 0

# Hangfire job failure rate spikes
increase(hangfire_jobs_failed_total[1m]) > 0
```

**Recovery Validation:**
*   Once the firewall rule is removed, the ADO.NET connection pool must automatically heal.
*   Hangfire's Exponential Backoff correctly catches the failed processing jobs and successfully executes them within 5 minutes.

---

## 3. SCENARIO 2: HANGFIRE WORKER CRASH / SHUTDOWN

**Description:** Simulates out-of-memory worker crashes or pod evictions where the API is alive but background processing is completely halted.

**Simulation Method (.NET Chaos Hook):**
```csharp
// Endpoint: POST /api/chaos/hangfire/stop
// Requires appsettings specific flag
[HttpPost("hangfire/stop")]
public IActionResult StopWorkers() {
    _backgroundJobServer.SendStop(); // Gracefully stops local processing
    return Ok("Workers stopped for 120 seconds.");
}
```

**System Behavior Validation:**
*   **Must continue working:** Immediate API responses (Ticket creations) must retain < 200ms P95 latency.
*   **Allowed to degrade:** Audit logs and archival jobs are backed up.
*   **MUST NEVER happen:** The primary API becomes slow/unresponsive.

**Metric-Based Verification (PromQL):**
```promql
# Active servers drop to 0
hangfire_active_servers == 0

# Queue depth begins to climb linearly
rate(hangfire_queue_depth_default[1m]) > 0
```

**Recovery Validation:**
*   Once workers are restarted, `hangfire_queue_depth_default` should drain steadily.
*   `audit_log_processed_success_total` should spike heavily as the backlog is bulk-processed via TVP.

---

## 4. SCENARIO 3: QUEUE SATURATION (100K+ JOBS)

**Description:** An extreme burst event (e.g., opening system for thousands of students simultaneously) floods the system faster than it can write to the database.

**Simulation Method (K6 / Autocannon):**
Run the load-test script bypassing the API throttling rule natively, forcing 1,000 RPS for 2 minutes directly to the API ingestion endpoints.

**System Behavior Validation:**
*   **Must continue working:** Hangfire maintains job idempotency and enqueueing performance.
*   **Allowed to degrade:** End-to-end latency until an audit log appears in the DB can degrade from milliseconds to several minutes.
*   **MUST NEVER happen:** Buffer overflow crashing the application. Lock escalation on SQL Server when Hangfire tries to process massive batches limitlessly.

**Metric-Based Verification (PromQL):**
```promql
# Identify the backlog
hangfire_queue_depth_default > 50000

# Ensure processing continues smoothly (not crashing)
rate(audit_log_processed_success_total[1m]) > 100
```

**Recovery Validation:**
*   Worker threads do not time out. The queue naturally drains over time without human intervention. Zero `SqlException` occurrences in the logs.

---

## 5. SCENARIO 4: DISK FULL (LOG/ARCHIVE FAILURE)

**Description:** Simulates what happens when Serilog or `LocalGzipAuditArchiveProvider` runs out of space.

**Simulation Method (PowerShell/Bash):**
```bash
# Create a massive dummy file to fill the container/drive completely
fallocate -l 100G /app/Logs/dummy_fill.img
# Wait 2 minutes, then delete
rm /app/Logs/dummy_fill.img
```

**System Behavior Validation:**
*   **Must continue working:** The API must still return successful HTTP responses. Audit logs are still written to SQL Server.
*   **Allowed to degrade:** Serilog silent drops (assuming `WriteTo.File` is configured asynchronously and swallows exceptions). Archival jobs fail.
*   **MUST NEVER happen:** The API returning HTTP 500s strictly because a local log file couldn't be written.

**Metric-Based Verification (PromQL):**
```promql
# Alert on Host Free Space
node_filesystem_avail_bytes{mountpoint="/"} / node_filesystem_size_bytes{mountpoint="/"} < 0.05
```

**Recovery Validation:**
*   Clear the dummy file.
*   Trigger `AuditRetentionJob` manually via Hangfire Dashboard. Ensure it successfully catches up and writes the pending GZip archives.

---

## 6. SCENARIO 5: HIGH GC / LOH PRESSURE

**Description:** Validates system stability when a malicious or buggy client sends insanely large JSON payloads.

**Simulation Method (.NET App / K6):**
Send a POST request where a single string field is padded to 2 MB of text, bypassing normal norms, or trigger an endpoint specifically designed to allocate large byte arrays.
```csharp
[HttpPost("chaos/memory")]
public IActionResult SpikeMemory() {
    byte[] spike = new byte[100 * 1024 * 1024]; // 100MB allocation
    return Ok();
}
```

**System Behavior Validation:**
*   **Must continue working:** Subsequent requests should proceed.
*   **Allowed to degrade:** Minor API latency spike during Gen2 garbage collection.
*   **MUST NEVER happen:** `OutOfMemoryException` crashing the entire Kestrel web server instance.

**Metric-Based Verification (PromQL):**
```promql
# Monitor LOH climbing
dotnet_gc_heap_size_bytes{generation="loh"} > 50000000

# Monitor GC Time
dotnet_gc_time_percentage > 10
```

**Recovery Validation:**
*   Stop the large payloads. Wait 60 seconds.
*   Metric `dotnet_gc_heap_size_bytes{generation="loh"}` should drop back to baseline (~6MB) indicating successful memory reclamation.

---

## 7. SCENARIO 6: METRICS UNAVAILABLE (PROMETHEUS DOWN)

**Description:** The observability stack goes dark. Does the application panic?

**Simulation Method:**
Stop the Prometheus scraper or block the `/metrics` endpoint via API Gateway routing rules.

**System Behavior Validation:**
*   **Must continue working:** The core business application must function flawlessly.
*   **Allowed to degrade:** Observability blindness.
*   **MUST NEVER happen:** The OpenTelemetry SDK buffering metrics in memory infinitely until it causes an OOM crash.

**Recovery Validation:**
*   Restore Prometheus scraper.
*   Metrics should instantly repopulate without requiring an API restart.
