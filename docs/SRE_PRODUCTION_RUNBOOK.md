# Rimer API: Production Operations & Reliability Runbook
**Classification:** Internal SRE & Operations Document  
**Target Audience:** SREs, DevOps, L2/L3 Support Engineers, Systems Architects  
**System Scope:** Rimer API (ASP.NET Core, Hangfire, SQL Server TVP, OpenTelemetry)

---

## 1. INCIDENT RESPONSE RUNBOOK (CRITICAL)

This section details critical failure modes, their observability signals, and the standard operating procedures (SOP) for mitigation.

### 1.1 High Queue Depth (Hangfire Backlog)
*   **Symptoms:** `hangfire_queue_depth_default` metric exceeds 5,000 and is linearly climbing. API continues to return HTTP 2xx, but audit logs are delayed.
*   **Root Cause Possibilities:** 
    *   SQL Server I/O bottleneck slowing down TVP bulk inserts.
    *   Sudden traffic burst exceeding processing capacity (>500 RPS).
    *   Hangfire worker thread starvation.
*   **Resolution Actions:**
    1.  **Monitor:** Check SQL Server CPU and `PAGEIOLATCH_EX` wait stats.
    2.  **Mitigate:** Temporarily scale up API container instances/pods to increase total Hangfire worker count.
    3.  **Halt Consumers:** If DB is at 100% CPU, pause non-critical background jobs via Hangfire Dashboard.
    4.  **Investigate:** Identify if the global rate limit was bypassed.

### 1.2 SQL Timeouts / DB Slowdowns
*   **Symptoms:** High DB latency (>100ms per transaction). `Microsoft.Data.SqlClient.SqlException` (Timeout) appearing in application logs.
*   **Root Cause Possibilities:**
    *   `AuditLogs` table lock escalation (e.g., from an ad-hoc query or the recovery job hitting index limits).
    *   Disk IOPS saturation.
    *   Connection pool exhaustion.
*   **Resolution Actions:**
    1.  **Identify Blocker:** Connect to SQL Server and run `sp_WhoIsActive` to find blocking SPIDs.
    2.  **Kill Session:** Kill the blocking read/update query if it is an ad-hoc report.
    3.  **Tuning:** Verify that the `AuditLogRecoveryJob` is using the `(Status, CreatedAt)` composite index properly.

### 1.3 Worker Crash / Hangfire Stoppage
*   **Symptoms:** Queue depth rises but processing rate (`audit_log_processed_success_total` rate) drops to zero. Process restart loops detected.
*   **Root Cause Possibilities:**
    *   Application Out Of Memory (OOM) due to massive memory leak.
    *   Corrupted TVP payload crashing the thread.
    *   OS/Kubernetes pod eviction.
*   **Resolution Actions:**
    1.  **Restart:** Perform a rolling restart of the API instances.
    2.  **Quarantine:** If a specific job payload is causing a crash loop, delete the job via Hangfire Dashboard.
    3.  **Analyze Logs:** Check OS event logs and Serilog fatal logs immediately preceding the crash.

### 1.4 Memory Pressure / LOH Growth
*   **Symptoms:** `generation="loh"` metric exceeds 50MB and steadily grows. High % Time in GC. Increased API P99 latency.
*   **Root Cause Possibilities:**
    *   Large JSON payloads bypassing validation and entering the TVP `DataTable`.
    *   String concatenations of large requests in memory.
*   **Resolution Actions:**
    1.  **Drop Traffic:** Temporarily lower global rate limits to relieve GC pressure.
    2.  **Dump:** Capture a memory dump (`dotnet-dump collect -p <PID>`) for post-mortem analysis.
    3.  **Restart:** Graceful restart of instances showing >85% memory utilization.

### 1.5 Disk Full / Log Overflow
*   **Symptoms:** "No space left on device" errors. Serilog stops writing `log-YYYYMMDD.txt`. API returns HTTP 500s on file I/O operations.
*   **Root Cause Possibilities:**
    *   Error logging flood (error amplification).
    *   `AuditRetentionJob` failed to purge old records or GZip archives.
*   **Resolution Actions:**
    1.  **Clear Space:** Truncate or compress the largest text logs in the `./Logs` directory.
    2.  **Verify Retention:** Manually trigger `AuditRetentionJob` to clear SQL space and delete old GZip cold archives.
    3.  **Filter:** Dynamically adjust `appsettings.json` `MinimumLevel` to `Fatal` to stop log flooding until the root cause is fixed.

### 1.6 Audit Log Processing Delay (Stale Data)
*   **Symptoms:** System is functioning, but audit logs from 15 minutes ago are not visible in the DB.
*   **Root Cause Possibilities:**
    *   Records stuck in `Pending` state. `AuditLogRecoveryJob` not firing or crashing silently.
*   **Resolution Actions:**
    1.  **Dashboard Route:** Open Hangfire Dashboard -> 'Recurring Jobs'.
    2.  **Force Run:** Trigger `AuditLogRecoveryJob` manually.
    3.  **Check Status:** Execute `SELECT COUNT(*) FROM AuditLogs WHERE Status = 0` (0 = Pending) to measure the stale data size.

### 1.7 Rate Limiter Saturation (429 Spikes)
*   **Symptoms:** Unusually high rate of HTTP 429 Too Many Requests.
*   **Root Cause Possibilities:**
    *   DDoS attack.
    *   Misconfigured load test.
    *   A massive batch background process hitting the API abruptly.
*   **Resolution Actions:**
    1.  **Identify:** Group 429 errors by Client IP.
    2.  **Block:** Ban malicious IPs at the WAF / Reverse Proxy layer.
    3.  **Adjust:** If it's legitimate internal traffic, temporarily increase the limit (from 100k/min to needed capacity) if infrastructure allows.

---

## 2. GRAFANA DASHBOARD DESIGN

A production-ready Grafana dashboard based on OpenTelemetry and Prometheus exported data.

### 2.1 API Performance Overview
*   **Throughput (RPS):**
    *   *Metric:* `sum(rate(http_requests_received_total[1m]))`
    *   *Description:* Total API requests per second.
    *   *Alert:* `Warning` at > 350 RPS, `Critical` at > 450 RPS.
*   **P95 Latency:**
    *   *Metric:* `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))`
    *   *Description:* Latency experienced by 95% of users.
    *   *Alert:* `Warning` > 300ms, `Critical` > 500ms.
*   **Error Rate (5xx):**
    *   *Metric:* `sum(rate(http_requests_received_total{code=~"5.."}[1m])) / sum(rate(http_requests_received_total[1m]))`
    *   *Description:* Percentage of requests resulting in server errors.
    *   *Alert:* `Critical` > 1%.

### 2.2 Audit Pipeline Health
*   **Audit Throughput:**
    *   *Metric:* `rate(audit_log_processed_success_total[1m])`
    *   *Description:* Speed of the TVP Bulk Insert.
*   **Audit Failures:**
    *   *Metric:* `increase(audit_log_processed_failure_total[1m])`
    *   *Description:* Absolute count of failed bulk inserts.
    *   *Alert:* `Critical` > 0.

### 2.3 Queue Metrics
*   **Hangfire Queue Depth:**
    *   *Metric:* `hangfire_queue_depth_default`
    *   *Description:* Total pending jobs. Indicates if ingestion is outpacing processing.
    *   *Alert:* `Warning` > 5,000, `Critical` > 15,000.

### 2.4 Database Performance
*   **Connection Pool Saturation:**
    *   *Description:* Active/Idle connections in the pool.
    *   *Alert:* `Warning` > 80% capacity.
*   **SQL Command Execution Time:**
    *   *Description:* Average execution time for the TVP Insert command.
    *   *Alert:* `Warning` > 200ms.

### 2.5 System Runtime Health
*   **LOH (Large Object Heap) Size:**
    *   *Metric:* `dotnet_gc_heap_size_bytes{generation="loh"}`
    *   *Description:* Memory footprint of large objects.
    *   *Alert:* `Warning` > 50MB.
*   **GC Collections (Gen0/Gen1/Gen2):**
    *   *Description:* Rate of garbage collections. High Gen2 indicates memory stress.
*   **Process CPU / Memory Usage:**
    *   *Alert:* `Warning` CPU > 85%, Pod Memory > 80%.

---

## 3. ALERTING STRATEGY

Alerts must be actionable and routed via PagerDuty, OpsGenie, or Slack channels based on severity.

| Alert Name | Severity | Condition (PromQL/Threshold) | Duration | Recommended SRE Action |
| :--- | :--- | :--- | :--- | :--- |
| **AuditQueueBacklog** | Warning | `hangfire_queue_depth_default > 5000` | 2m | Check DB latency; prepare to scale workers. |
| **AuditQueueSaturation**| Critical | `hangfire_queue_depth_default > 15000` | 5m | Incident response. DB IO is likely saturated. |
| **AuditWriteFailure** | Critical | `increase(audit_log_processed_failure[2m]) > 0` | 0m | Immediate investigation into `SqlException` logs. |
| **HighErrorRate** | Critical | `Error rate > 1%` | 3m | Identify failing endpoint. Check DB connectivity. |
| **ApiLatencyElevated** | Warning | `P95 Latency > 300ms` | 5m | Monitor LOH and GC. Check for noisy neighbors. |
| **MemoryPressure** | Warning | `LOH > 50MB` or `% Time in GC > 10%` | 5m | Correlate with traffic payload sizes. Prepare instance restart. |
| **DiskSpaceLow** | Critical | `Free space < 10%` | 2m | Trigger retention jobs; manual log rotation. |

---

## 4. CHAOS TEST PLAN (FAILURE SIMULATION)

These are controlled failure scenarios (Game Days) to guarantee the resilience mechanisms (backpressure, retry policies, Hangfire idempotency) function properly in production.

### 4.1 Stop SQL Server
*   **Action:** Take the database offline or block port 1433 for 60 seconds.
*   **Expected Behavior:** API fails gracefully (503s or specific error messages). Hangfire jobs fail and back off exponentially. Queue depth grows safely.
*   **MUST NOT Happen:** Data loss. App crashing entirely (OOM). 
*   **Validation:** Once DB is restored, Hangfire must retry and successfully persist 100% of the queued jobs within 5 minutes.

### 4.2 Kill Hangfire Workers
*   **Action:** Forcefully kill the background worker threads or scale worker count to 0.
*   **Expected Behavior:** API continues accepting requests and queuing them. Queue depth increases.
*   **MUST NOT Happen:** HTTP endpoints blocking/timing out waiting for background jobs to finish.
*   **Validation:** Upon worker restoration, queue drains steadily without creating lock escalation in SQL.

### 4.3 Network Latency Injection
*   **Action:** Introduce 200ms of latency between the API and SQL Server.
*   **Expected Behavior:** TVP inserts take longer. Queue drains slower. P95 API latency might marginally increase if synchronous queries are affected.
*   **MUST NOT Happen:** Connection pool exhaustion entirely locking up the system.
*   **Validation:** Throttling (Rate Limiting) and timeout policies successfully prevent complete systemic collapse.

### 4.4 Fill Disk Subsystem
*   **Action:** Artificially fill the disk where Serilog writes and the GZip archives are stored to 100%.
*   **Expected Behavior:** Async logging might drop logs or error out. File-based archival fails.
*   **MUST NOT Happen:** The primary HTTP processing path or DB connection crashes entirely.
*   **Validation:** Restore disk space, ensure system normalizes, trigger `AuditRetentionJob` manually to clear pending archives.

---

## 5. OPERATIONS CHECKLIST

### 5.1 Daily SRE Checks (Shift Handoff)
*   [ ] **Grafana Review:** Review 24h trend of P95 Latency and RPS.
*   [ ] **Queue Health:** Verify `hangfire_queue_depth_default` is currently 0 (or baseline).
*   [ ] **Error Logs:** Check Serilog for any "unhandled exceptions" or `SqlException`.
*   [ ] **DB Storage:** Check SQL Server disk usage trend.

### 5.2 Weekly Maintenance Tasks
*   [ ] **Retention Audit:** Verify the `./AuditArchives/` size. Ensure files older than 7 days are successfully deleted off-disk.
*   [ ] **Hangfire Housekeeping:** Clean up 'Deleted' or 'Succeeded' job history artifacts from the Hangfire dashboard to keep the metadata DB light.
*   [ ] **Capacity Review:** Compare the week's peak RPS against the 400 RPS theoretical limit. If week's peak > 250 RPS, begin capacity planning for node scale-up.

### 5.3 Monthly / Release Tasks
*   [ ] **Chaos Run:** Execute at least one chaos testing scenario (e.g., worker restart during load).
*   [ ] **Performance Baseline:** Run `autocannon-test.js` (Phase 1 Baseline) against Staging to ensure new code hasn't regressed P95 latency.
*   [ ] **Index Fragmentation:** Run query against SQL Server to check `AuditLogs` index fragmentation; rebuild if > 30%.
