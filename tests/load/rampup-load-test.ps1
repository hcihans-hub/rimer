param(
    [int]$DurationSeconds = 180
)

$BaseUrl = "http://localhost:5000"
$DeptId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

# Fresh token
$loginResp = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body '{"email":"admin@rimer.edu","password":"Admin@123456"}'
$Token = $loginResp.data.token
Write-Host "Token acquired."

function Get-Metrics {
    try {
        $raw = (Invoke-WebRequest -Uri "$BaseUrl/metrics" -UseBasicParsing -ErrorAction Stop).Content
        $success = if ($raw -match 'audit_log_processed_success_total\{[^}]+\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $failure = if ($raw -match 'audit_log_processed_failure_total\{[^}]+\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $qdepth = if ($raw -match 'hangfire_queue_depth_default\{[^}]+\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $loh = if ($raw -match 'generation="loh"[^}]*\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        $gen0 = if ($raw -match 'gc_collections_count_total\{[^}]+generation="gen0"[^}]*\}\s+([\d.]+)') { [double]$Matches[1] } else { 0 }
        [pscustomobject]@{ Success = $success; Failure = $failure; QDepth = $qdepth; LOH = $loh; Gen0 = $gen0 }
    }
    catch { [pscustomobject]@{ Success = 0; Failure = 0; QDepth = -1; LOH = 0; Gen0 = 0 } }
}

$before = Get-Metrics

# Ramp-up plan: 3 phases over 180 seconds
# Phase A: 0-60s   → 100 RPS (warm-up)
# Phase B: 60-120s → 200 RPS (sustained pressure)
# Phase C: 120-180s → 400 RPS (stress spike)
$phases = @(
    [pscustomobject]@{ Name = "Phase A: Warm-up"; RPS = 100; StartSec = 0; EndSec = 60 }
    [pscustomobject]@{ Name = "Phase B: Pressure"; RPS = 200; StartSec = 60; EndSec = 120 }
    [pscustomobject]@{ Name = "Phase C: Stress"; RPS = 400; StartSec = 120; EndSec = 180 }
)

Write-Host ""
Write-Host "============================================"
Write-Host " RAMP-UP LOAD TEST  100→200→400 RPS / ${DurationSeconds}s"
Write-Host "============================================"
Write-Host "[BEFORE] success=$($before.Success) | failure=$($before.Failure) | qDepth=$($before.QDepth) | LOH=$([math]::Round($before.LOH/1MB,2))MB"
Write-Host ""

# Runspace pool for true parallelism
$pool = [runspacefactory]::CreateRunspacePool(1, 50)
$pool.Open()

$scriptBlock = {
    param($url, $token, $body)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }
        $r = [System.Net.WebRequest]::Create("$url/api/tickets")
        $r.Method = "POST"
        $r.ContentType = "application/json"
        $r.Headers.Add("Authorization", "Bearer $token")
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        $r.ContentLength = $bytes.Length
        $reqStream = $r.GetRequestStream()
        $reqStream.Write($bytes, 0, $bytes.Length)
        $reqStream.Close()
        $resp = $r.GetResponse()
        $code = [int]$resp.StatusCode
        $resp.Close()
        $sw.Stop()
        return @{ Code = $code; Ms = $sw.ElapsedMilliseconds }
    }
    catch {
        $sw.Stop()
        return @{ Code = 0; Ms = $sw.ElapsedMilliseconds }
    }
}

$allHandles = [System.Collections.Generic.List[object]]::new()
$successCount = 0
$errorCount = 0
$latencies = [System.Collections.Generic.List[double]]::new()
$startTime = [datetime]::UtcNow
$reqIndex = 0
$lastReport = -1

for ($sec = 0; $sec -lt $DurationSeconds; $sec++) {
    $secStart = [datetime]::UtcNow

    # Determine current RPS from ramp plan
    $currentRps = 50
    $phaseName = "Unknown"
    foreach ($p in $phases) {
        if ($sec -ge $p.StartSec -and $sec -lt $p.EndSec) {
            $currentRps = $p.RPS
            $phaseName = $p.Name
            break
        }
    }

    # Launch $currentRps parallel requests
    $secHandles = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $currentRps; $i++) {
        $reqIndex++
        $body = "{""title"":""Ramp Ticket $reqIndex"",""description"":""Ramp-up test"",""departmentId"":""$DeptId"",""priority"":1}"
        $ps = [powershell]::Create().AddScript($scriptBlock).AddArgument($BaseUrl).AddArgument($Token).AddArgument($body)
        $ps.RunspacePool = $pool
        $handle = $ps.BeginInvoke()
        $secHandles.Add([pscustomobject]@{ PS = $ps; Handle = $handle })
    }

    # Collect completed from THIS second
    foreach ($h in $secHandles) {
        try {
            $result = $h.PS.EndInvoke($h.Handle)
            if ($result -and $result.Code -ge 200 -and $result.Code -lt 300) {
                $successCount++
                $latencies.Add([double]$result.Ms)
            }
            else {
                $errorCount++
            }
        }
        catch { $errorCount++ }
        $h.PS.Dispose()
    }

    # Progress every 10 seconds
    $elapsed = [math]::Floor(([datetime]::UtcNow - $startTime).TotalSeconds)
    if ([math]::Floor($elapsed / 10) -gt $lastReport) {
        $lastReport = [math]::Floor($elapsed / 10)
        $m = Get-Metrics
        Write-Host "[+${elapsed}s] [$phaseName] fired=$reqIndex | 2xx=$successCount | err=$errorCount | qDepth=$($m.QDepth) | audit=$($m.Success) | LOH=$([math]::Round($m.LOH/1MB,2))MB"
    }

    # Pace to 1 second
    $secMs = ([datetime]::UtcNow - $secStart).TotalMilliseconds
    $wait = 1000 - $secMs
    if ($wait -gt 0) { Start-Sleep -Milliseconds ([int]$wait) }
}

Write-Host "`n[...] Waiting 10s for Hangfire flush..."
Start-Sleep -Seconds 10

$pool.Close()
$pool.Dispose()

$after = Get-Metrics

# Stats
$sorted = $latencies | Sort-Object
$count = $sorted.Count
$p50 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.50), $count - 1)] } else { 0 }
$p95 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.95), $count - 1)] } else { 0 }
$p99 = if ($count -gt 0) { $sorted[[math]::Min([int]($count * 0.99), $count - 1)] } else { 0 }
$avg = if ($count -gt 0) { [math]::Round(($sorted | Measure-Object -Average).Average, 1) } else { 0 }
$actualRps = [math]::Round($successCount / $DurationSeconds, 1)
$dSuccess = $after.Success - $before.Success
$dFailure = $after.Failure - $before.Failure
$dLOH = [math]::Round(($after.LOH - $before.LOH) / 1MB, 2)
$dGen0 = $after.Gen0 - $before.Gen0

Write-Host ""
Write-Host "============================================"
Write-Host "         RAMP-UP TEST RESULTS               "
Write-Host "============================================"
Write-Host "Duration         : ${DurationSeconds}s"
Write-Host "Requests Fired   : $reqIndex"
Write-Host "HTTP 2xx         : $successCount"
Write-Host "Errors           : $errorCount"
Write-Host "Actual RPS       : $actualRps"
Write-Host ""
Write-Host "--- Latency ---"
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
Write-Host "Gen0 GC Cycles   : $dGen0"
Write-Host "============================================"

Write-Host ""
Write-Host "--- VALIDATION ---"
if ($after.QDepth -lt 10000) { Write-Host "[PASS] Queue depth < 10K (no backpressure triggered): $($after.QDepth)" } else { Write-Host "[INFO] Queue depth elevated (backpressure zone): $($after.QDepth)" }
if ($dFailure -eq 0) { Write-Host "[PASS] Zero audit failures" }                                              else { Write-Host "[WARN] Audit failures: $dFailure" }
if ($p95 -lt 1000) { Write-Host "[PASS] P95 < 1000ms: ${p95}ms" }                                           else { Write-Host "[WARN] P95 elevated: ${p95}ms" }
if ($dLOH -lt 100) { Write-Host "[PASS] LOH growth < 100MB: ${dLOH}MB" }                                   else { Write-Host "[WARN] LOH growth: ${dLOH}MB" }
if ($errorCount -lt ($reqIndex * 0.05)) { Write-Host "[PASS] Error rate < 5%: $([math]::Round($errorCount/$reqIndex*100,1))%" } else { Write-Host "[WARN] Error rate: $([math]::Round($errorCount/$reqIndex*100,1))%" }
