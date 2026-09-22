param(
    [int]$TargetRps      = 50,
    [int]$DurationSeconds = 120
)

$BaseUrl = "http://localhost:5000"
$DeptId  = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

# Fresh token
$loginResp = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@rimer.edu","password":"Admin@123456"}'
$Token = $loginResp.data.token
Write-Host "Token acquired: $($Token.Substring(0,20))..."

### ── Helper: fetch metrics ──────────────────────────────────────────────
function Get-Metrics {
    try {
        $raw = (Invoke-WebRequest -Uri "$BaseUrl/metrics" -UseBasicParsing -ErrorAction Stop).Content
        $success = if ($raw -match 'audit_log_processed_success_total\{[^}]+\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $failure = if ($raw -match 'audit_log_processed_failure_total\{[^}]+\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $qdepth  = if ($raw -match 'hangfire_queue_depth_default\{[^}]+\}\s+([\d.]+)')       { [double]$Matches[1] } else { 0 }
        $loh     = if ($raw -match 'generation="loh"[^}]*\}\s+([\d.]+)')                     { [double]$Matches[1] } else { 0 }
        $gen0    = if ($raw -match 'gc_collections_count_total\{[^}]+generation="gen0"[^}]*\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $alloc   = if ($raw -match 'gc_allocations_size_bytes_total\{[^}]+\}\s+([\d.]+)')    { [double]$Matches[1] } else { 0 }
        [pscustomobject]@{ Success=$success; Failure=$failure; QDepth=$qdepth; LOH=$loh; Gen0=$gen0; AllocBytes=$alloc }
    } catch { [pscustomobject]@{ Success=0; Failure=0; QDepth=-1; LOH=0; Gen0=0; AllocBytes=0 } }
}

### ── BEFORE snapshot ────────────────────────────────────────────────────
$before = Get-Metrics
Write-Host ""
Write-Host "============================================"
Write-Host " BASELINE LOAD TEST  $TargetRps RPS / ${DurationSeconds}s"
Write-Host "============================================"
Write-Host "[BEFORE] success=$($before.Success) | failure=$($before.Failure) | qDepth=$($before.QDepth) | LOH=$([math]::Round($before.LOH/1MB,2))MB"
Write-Host ""

### ── Sequential burst sender (reliable, no Start-Job overhead) ──────────
$successCount = 0
$errorCount   = 0
$latencies    = [System.Collections.Generic.List[double]]::new()
$startTime    = [datetime]::UtcNow
$endTime      = $startTime.AddSeconds($DurationSeconds)
$reqIndex     = 0
$headers      = @{ Authorization = "Bearer $Token"; "Content-Type" = "application/json" }
$lastReport   = 0

while ([datetime]::UtcNow -lt $endTime) {
    $batchStart = [datetime]::UtcNow

    # Fire burst of requests sequentially within each second
    for ($i = 0; $i -lt $TargetRps; $i++) {
        $reqIndex++
        $body = "{""title"":""BL Ticket $reqIndex"",""description"":""Baseline load"",""departmentId"":""$DeptId"",""priority"":1}"
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $r = Invoke-WebRequest -Uri "$BaseUrl/api/tickets" -Method Post -Headers $headers -Body $body -UseBasicParsing -ErrorAction Stop
            $sw.Stop()
            $successCount++
            $latencies.Add($sw.ElapsedMilliseconds)
        } catch {
            $sw.Stop()
            $errorCount++
        }
    }

    # Progress report every ~10 seconds
    $elapsed = [math]::Floor(([datetime]::UtcNow - $startTime).TotalSeconds)
    if ($elapsed -ge ($lastReport + 10)) {
        $lastReport = $elapsed
        $m = Get-Metrics
        Write-Host "[+${elapsed}s] fired=$reqIndex | 2xx=$successCount | err=$errorCount | qDepth=$($m.QDepth) | audit_success=$($m.Success) | LOH=$([math]::Round($m.LOH/1MB,2))MB"
    }

    # Pace to ~1 second per burst
    $batchMs = ([datetime]::UtcNow - $batchStart).TotalMilliseconds
    $wait = 1000 - $batchMs
    if ($wait -gt 0) { Start-Sleep -Milliseconds ([int]$wait) }
}

# Let Hangfire flush remaining jobs
Write-Host "`n[...] Waiting 5s for Hangfire to process remaining jobs..."
Start-Sleep -Seconds 5

### ── AFTER snapshot ─────────────────────────────────────────────────────
$after = Get-Metrics

### ── Results ────────────────────────────────────────────────────────────
$sorted  = $latencies | Sort-Object
$count   = $sorted.Count
$p50 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.50), $count-1)] } else { 0 }
$p95 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.95), $count-1)] } else { 0 }
$p99 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.99), $count-1)] } else { 0 }
$avg = if ($count -gt 0) { [math]::Round(($sorted | Measure-Object -Average).Average, 1) } else { 0 }
$actualRps   = [math]::Round($successCount / $DurationSeconds, 1)
$dSuccess    = $after.Success - $before.Success
$dFailure    = $after.Failure - $before.Failure
$dLOH        = [math]::Round(($after.LOH - $before.LOH) / 1MB, 2)
$dGen0       = $after.Gen0 - $before.Gen0
$dAllocMB    = [math]::Round(($after.AllocBytes - $before.AllocBytes) / 1MB, 1)

Write-Host ""
Write-Host "============================================"
Write-Host "         BASELINE TEST RESULTS              "
Write-Host "============================================"
Write-Host "Duration         : ${DurationSeconds}s"
Write-Host "Requests Fired   : $reqIndex"
Write-Host "HTTP 2xx         : $successCount"
Write-Host "Errors           : $errorCount"
Write-Host "Actual RPS       : $actualRps"
Write-Host ""
Write-Host "--- Latency (HTTP round-trip) ---"
Write-Host "Avg              : ${avg}ms"
Write-Host "P50              : ${p50}ms"
Write-Host "P95              : ${p95}ms"
Write-Host "P99              : ${p99}ms"
Write-Host ""
Write-Host "--- Audit Metrics (delta) ---"
Write-Host "Logs Inserted    : $dSuccess"
Write-Host "Log Failures     : $dFailure"
Write-Host "Queue Depth (end): $($after.QDepth)"
Write-Host ""
Write-Host "--- GC / Memory (delta) ---"
Write-Host "LOH Growth       : ${dLOH}MB"
Write-Host "Total Alloc      : ${dAllocMB}MB"
Write-Host "Gen0 GC Cycles   : $dGen0"
Write-Host "============================================"

Write-Host ""
Write-Host "--- VALIDATION ---"
if ($after.QDepth -lt 500)  { Write-Host "[PASS] Queue depth stable: $($after.QDepth)" }         else { Write-Host "[WARN] Queue depth elevated: $($after.QDepth)" }
if ($dFailure -eq 0)        { Write-Host "[PASS] Zero audit failures" }                            else { Write-Host "[FAIL] Audit failures: $dFailure" }
if ($p95 -lt 500)           { Write-Host "[PASS] P95 latency < 500ms: ${p95}ms" }                  else { Write-Host "[WARN] P95 latency high: ${p95}ms" }
if ($dLOH -lt 50)           { Write-Host "[PASS] LOH growth < 50MB: ${dLOH}MB" }                  else { Write-Host "[WARN] LOH growth elevated: ${dLOH}MB" }
if ($errorCount -eq 0)      { Write-Host "[PASS] Zero HTTP errors" }                               else { Write-Host "[WARN] HTTP errors: $errorCount" }
