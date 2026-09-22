# EXECUTABLE RUNBOOK: Rimer API Operations
**Classification:** CRITICAL / INCIDENT RESPONSE  
**System Scope:** ASP.NET Core, Hangfire, SQL Server TVP, OpenTelemetry

---

## 1. HANGFIRE QUEUE BACKLOG SPIKE

### 1.1 EXACT DETECTION
**PromQL (Queue Depth Alert):**
```promql
hangfire_queue_depth_default > 10000
```
**PromQL (Check if ingestion has stopped):**
```promql
rate(audit_log_processed_success_total[2m]) == 0
```

### 1.2 STEP-BY-STEP TRIAGE COMMANDS
**SQL: Check oldest stuck jobs to estimate delay:**
```sql
SELECT TOP 10 Id, StateName, CreatedAt 
FROM HangFire.Job WITH (NOLOCK) 
WHERE StateName = 'Enqueued'
ORDER BY CreatedAt ASC;
```

**SQL: Check worker server health:**
```sql
SELECT Id, WorkerCount, LastHeartbeat 
FROM HangFire.Server WITH (NOLOCK);
```

### 1.3 IMMEDIATE MITIGATION ACTIONS
1. Go to Hangfire Dashboard: `https://<api-url>/hangfire`.
2. Navigate to **Recurring Jobs** and temporarily **Pause** `AuditLogRecoveryJob` and `AuditRetentionJob` to free up SQL Server IOPS.
3. If CPU utilizes < 70%, horizontally scale the API instance by +1 pod/node to increase the total worker pool.

### 1.4 ROOT CAUSE DEBUGGING
1. Check if the spike correlates with HTTP traffic: `sum(rate(http_requests_received_total[1m])) > 400`. If yes, it's an expected traffic burst.
2. If traffic is normal, check DB blocks (See Scenario 2).

### 1.5 SAFE RECOVERY STEPS
1. Wait for the queue to naturally drain. Hangfire's idempotency ensures data persistence.
2. Once `hangfire_queue_depth_default < 1000`, resume any paused recurring jobs.

### 1.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** restart application instances to "clear" the queue. The queue is stored in SQL; restarts abort active TVP batches, trigger DB rollbacks, and make the backlog worse.
*   **DO NOT** manually delete records from `HangFire.Job`.

---

## 2. SQL TIMEOUT / DB SLOWDOWNS

### 2.1 EXACT DETECTION
**PromQL (High Latency):**
```promql
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{route="api/audit"}[2m])) > 1.5
```
**Metrics to Check:** Count of `Microsoft.Data.SqlClient.SqlException` in structured logs.

### 2.2 STEP-BY-STEP TRIAGE COMMANDS
**SQL: Find exactly what is blocking TVP inserts (Run as sa):**
```sql
EXEC sp_WhoIsActive
    @get_locks = 1,
    @get_task_info = 2,
    @find_block_leaders = 1;
```

**SQL: Check current active transactions on AuditLogs:**
```sql
SELECT db.name, req.start_time, req.status, req.command, sqltext.text
FROM sys.dm_exec_requests req
CROSS APPLY sys.dm_exec_sql_text(req.sql_handle) sqltext
JOIN sys.databases db ON req.database_id = db.database_id
WHERE sqltext.text LIKE '%AuditLogs%';
```

### 2.3 IMMEDIATE MITIGATION ACTIONS
1. If the blocking query is an ad-hoc SELECT (e.g., a dev/analyst running a query without `NOLOCK`), **KILL it** immediately: `KILL <SPID>;`
2. If the blocking query is the `AuditRetentionJob` (DELETE statement), kill the SPID and disable the job in Hangfire.

### 2.4 ROOT CAUSE DEBUGGING
1. Look at the `sp_WhoIsActive` output. If `wait_type` shows `PAGEIOLATCH_EX`, the disk subsystem is physically saturated.
2. If `wait_type` is `LCK_M_IX`, there is strict write contention.

### 2.5 SAFE RECOVERY STEPS
1. Allow the killed transactions to roll back.
2. Hangfire will automatically capture the timeouts and retry the inserts with Exponential Backoff. Do not intervene manually.

### 2.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** run database schema migrations (EF Core `Update-Database`) during active timeouts.
*   **DO NOT** rebuild indexes on `AuditLogs` while TVP batches are attempting inserts.

---

## 3. WORKER CRASH / HANGFIRE STUCK JOBS

### 3.1 EXACT DETECTION
**PromQL (Worker Death):**
```promql
hangfire_active_servers == 0
```
**PromQL (Jobs failing repeatedly):**
```promql
rate(hangfire_jobs_failed_total[5m]) > 5
```

### 3.2 STEP-BY-STEP TRIAGE COMMANDS
**SQL: Identify jobs stuck in 'Processing' state:**
```sql
SELECT Id, InvocationData 
FROM HangFire.Job WITH (NOLOCK)
WHERE StateName = 'Processing' 
AND DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) > 30;
```

**CLI: Check application exit codes:**
```bash
# If using Podman/Docker:
docker ps -a --filter "name=rimer-api"
docker logs <container_id> --tail 100 | grep "Fatal"
```

### 3.3 IMMEDIATE MITIGATION ACTIONS
1. If instances are in a crash loop, isolate traffic. Enable "Maintenance Mode" (HTTP 503) on the API Gateway to stop accepting new requests.
2. Re-queue the stuck processing jobs via Hangfire SQL:
```sql
-- Revert stuck processing jobs back to queue safely
UPDATE HangFire.Job 
SET StateName = 'Enqueued' 
WHERE StateName = 'Processing' AND DATEDIFF(MINUTE, CreatedAt, GETUTCDATE()) > 30;
```

### 3.4 ROOT CAUSE DEBUGGING
1. Review Serilog `Fatal` logs. Is it an `OutOfMemoryException`?
2. Ensure connection strings are resolving (DNS failure can cause massive Hangfire crashes).

### 3.5 SAFE RECOVERY STEPS
1. Restart the API instances one by one (Rolling Restart).
2. Manually trigger a very small job in Hangfire Dashboard to verify health.

### 3.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** leave stuck jobs in `Processing`. Hangfire will not retry them until the invisibility timeout (usually 30 mins) expires.

---

## 4. HIGH MEMORY / LOH GROWTH

### 4.1 EXACT DETECTION
**PromQL (LOH Alert):**
```promql
dotnet_gc_heap_size_bytes{generation="loh"} > 100000000
```
**PromQL (High CPU caused by GC):**
```promql
dotnet_gc_time_percentage > 25
```

### 4.2 STEP-BY-STEP TRIAGE COMMANDS
**CLI: Capture a GC / Memory Dump for analysis:**
```bash
# Identify PID
dotnet-counters monitor -n RimerApi.API --counters System.Runtime

# Create Dump
dotnet-dump collect -n RimerApi.API --type Heap
```

### 4.3 IMMEDIATE MITIGATION ACTIONS
1. The API is processing payloads larger than 85KB (LOH threshold).
2. Immediately throttle incoming POST sizes globally (e.g., config/Nginx limit: `client_max_body_size 50k;`).
3. Scale out instances to distribute memory load.

### 4.4 ROOT CAUSE DEBUGGING
1. Use `dotnet-dump analyze <dump_file>`.
2. Run command `dumpheap -stat -min 85000` to find which exact strings/arrays are breaching the LOH. 
3. Highly likely to be an abusive API client passing massive JSON payloads.

### 4.5 SAFE RECOVERY STEPS
1. Perform a controlled restart of the affected API instances to wipe the LOH.
2. Route traffic back once instances stabilize.

### 4.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** trigger `GC.Collect()` manually via diagnostic endpoints in production under high load; it freezes all execution threads.

---

## 5. DISK FULL / LOG OVERFLOW

### 5.1 EXACT DETECTION
**System Command / Alerts (Disk Space):**
```bash
df -h | grep /logs
```
*Application logs show `IOException: No space left on device` or Serilog stops outputting.*

### 5.2 STEP-BY-STEP TRIAGE COMMANDS
**CLI: Find the largest log files quickly:**
```bash
# Linux/Containers
find /app/Logs -type f -exec ls -lh {} + | awk '{ print $5, $9 }' | sort -hr | head -10

# PowerShell (Windows)
Get-ChildItem .\Logs -Recurse | Sort-Object Length -Descending | Select-Object Name, Length -First 10
```

### 5.3 IMMEDIATE MITIGATION ACTIONS
1. Force empty the current active error log to restore minimal operation:
```bash
# Linux
> /app/Logs/log-20260421.txt
```
```powershell
# Windows
Clear-Content .\Logs\log-20260421.txt
```
2. Compress (GZip) older application logs immediately.

### 5.4 ROOT CAUSE DEBUGGING
1. Open the tail of a log file: `tail -n 200 /app/Logs/log-20260421.txt`
2. Identify the repeating loop (Usually a failing DB connection or an infinite retry loop).

### 5.5 SAFE RECOVERY STEPS
1. Stop the error loop in code or config.
2. Run `AuditRetentionJob` manually via Hangfire to clear any old cold-storage `AuditArchives` from disk.

### 5.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** delete the current day's log file (e.g., `rm log-today.txt`). Serilog holds an active file handle and deleting the file will not free inodes, causing silent failure. Always truncate (`> file`) instead.

---

## 6. AUDIT PROCESSING DELAY (STUCK DATA)

### 6.1 EXACT DETECTION
**SQL: Calculate processing lag (Data delay):**
```sql
SELECT 
    COUNT(*) as BackloggedCount,
    MIN(CreatedAt) as OldestPendingRecord
FROM AuditLogs WITH (NOLOCK)
WHERE Status = 0; -- 0 represents Pending/Stuck
```
*If `OldestPendingRecord` is older than 5 minutes, you have a processing delay.*

### 6.2 STEP-BY-STEP TRIAGE COMMANDS
**SQL: Check if the recovery job ran successfully:**
```sql
SELECT TOP 5 StateName, CreatedAt, ExpireAt 
FROM HangFire.Job WITH (NOLOCK)
WHERE InvocationData LIKE '%AuditLogRecoveryJob%'
ORDER BY CreatedAt DESC;
```

### 6.3 IMMEDIATE MITIGATION ACTIONS
1. Open Hangfire Dashboard -> Recurring Jobs.
2. Manually trigger `AuditLogRecoveryJob`.

### 6.4 ROOT CAUSE DEBUGGING
1. Is an edge-case transaction inserting records without triggering the Hangfire queue logic? Look for code paths doing raw DB inserts rather than using the centralized MediatR command.

### 6.5 SAFE RECOVERY STEPS
1. Let the Recovery Job sweep the table. Monitor DB CPU spike while the sweep runs.

### 6.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** run an `UPDATE AuditLogs SET Status = 1` manually. Bypassing the queuing mechanism breaks idempotency and the event sourcing sequence.

---

## 7. RATE LIMITER SATURATION (429 SPIKES)

### 7.1 EXACT DETECTION
**PromQL (429 Spike Alert):**
```promql
rate(http_requests_received_total{code="429"}[1m]) > 50
```

### 7.2 STEP-BY-STEP TRIAGE COMMANDS
**CLI: Analyze logs to find top offending IPs:**
*(Assuming JSON format logs)*
```bash
grep "429" /app/Logs/log-today.txt | jq '.Properties.ClientIp' | sort | uniq -c | sort -nr | head -5
```

### 7.3 IMMEDIATE MITIGATION ACTIONS
1. If the top IPs are unknown/external, they are scraping/DDoS-ing the API. Add blocks to the Firewall / WAF immediately.
2. If the IPs belong to legitimate internal microservices or load testers, update `appsettings.json` dynamically to increase the rate limit (System supports up to 100k/min).

### 7.4 ROOT CAUSE DEBUGGING
1. Verify if global or client-specific limits are triggered. Examine the X-RateLimit-Policy headers returned in the response logs.

### 7.5 SAFE RECOVERY STEPS
1. Coordinate with the downstream service to enforce client-side Exponential Backoff and Jitter.

### 7.6 DO-NOT-DO (CRITICAL)
*   **DO NOT** disable Rate Limiting entirely. If you remove the rate limit during a flood, the SQL Server will become the immediate bottleneck, resulting in global system paralysis.
