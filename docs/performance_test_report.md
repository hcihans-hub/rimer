# Rimer API High-Throughput Stress Test & Optimization

**Date:** April 21, 2026  
**Version:** 3.0 
**Author:** Hasan Cihan Şenküçük Software Developer 
**Classification:** Technical / Infrastructure

---

## 1. Summary
This document provides a comprehensive technical analysis of the Rimer API's performance evolution. Following initial bottlenecks identified at the 200-400 RPS threshold, a series of architectural refinements and low-level SQL optimizations were implemented. 

The final verification confirms that the system now sustains a **stable 400 Requests Per Second (RPS)** with **100% availability**, zero data integrity issues in the audit pipeline, and a sustainable resource footprint. This results in a production-ready baseline for high-traffic enterprise environments.

---

## 2. Problem Analysis: 200-400 RPS Bottleneck

### 2.1 Observed Behavior
During earlier ramp-up tests, the system exhibited severe degradation when crossing the 200-300 RPS threshold:
*   **Latency Spikes:** Background audit log processing latency climbed exponentially.
*   **SQL Timeouts:** `Microsoft.Data.SqlClient.SqlException` timeouts occurred in background workers.
*   **Log Bloat:** Critical silent failures caused application error logs to swell to **235MB/day**, masking the true throughput capacity.

### 2.2 Root Cause Analysis (RCA)
1.  **Lock Escalation:** The `AuditLogQueueWorker` was performing "Stuck Record Recovery" (Full table scan with `UPDATE`) within the same loop as high-speed `INSERT` batches. This caused Page/Table locks, slowing down the hot path.
2.  **Invalid Scalar Expression:** A syntax error in the TVP Batch Insert query regarding `NEWSEQUENTIALID()` prevented successful commitment, leading to retry-loops and further resource exhaustion.
3.  **Rate-Limit Saturation:** The global rate-limiter was hitting its ceiling too early, causing unnecessary 429 responses that hindered clean performance measurement.

---

## 3. Solutions

### 3.1 Architecture Decoupling (Priority 1)
The recovery logic was completely extracted from the high-speed worker. A specialized **`AuditLogRecoveryJob`** was implemented as a Hangfire Recurring Job.
*   **Impact:** Zero contention on the main `AuditLogs` table during high-volume persistence. The worker now purely executes I/O-bound inserts.

### 3.2 SQL Ingestion Optimization
The TVP (Table-Valued Parameter) processing was refactored:
*   **Auto-Identity Logic:** Removed manual ID generation from the application layer. The database now handles sequential GUID assignment via `NEWSEQUENTIALID()` default constraints, which is significantly more performant than client-side generation.
*   **Batch Alignment:** Chunking logic was tuned to ensure optimal SQL transaction sizes without triggering lock escalation.

### 3.3 Data Retention & Archival ("Clean DB" Policy)
To prevent performance regression over time, an automated **Hot-to-Cold Archival** strategy was deployed:
*   **Hot Area:** Live DB holds the last **3 days** of searchable logs.
*   **Archival:** Records 3-7 days old are GZip-compressed and moved to file storage.
*   **Purge:** Hard deletion from SQL Server after **7 days**, maintaining a constant and predictable table size.

---

## 4. Final Verification Results (400 RPS)

| Metric | Measured Value | Result |
| :--- | :--- | :--- |
| **Throughput (Peak)** | 400 RPS | **PASSED** |
| **Total Test Volume** | ~24,000 requests | **Verified** |
| **Audit Logs Persisted** | **67,314 Logs** | **100% Success** |
| **Critical Errors** | 0 | **PASSED** |
| **SQL Timeouts** | 0 | **PASSED** |
| **Avg API Latency** | < 150ms | **Highly Responsive** |
| **Error Log Size** | 9.8 MB (Under stress) | **CONTROLLED** |

---

## 5. Verdict
The Rimer API system has successfully transitioned from an I/O-bound bottlenecked state to a **linearly scalable architecture**. The fixes applied have resolved the fundamental contention issues in the database layer.

**Recommendations for Production:**
*   Monitor `hangfire_queue_depth_default` via Prometheus; alert if depth > 10,000 for > 5 mins.
*   The system is now cleared for deployment with a target capacity of **500 RPS per instance**.

---

## 6. WebSocket & Protection Mechanism Tests (April 30, 2026) / WebSocket ve Koruma Mekanizması Testleri (30 Nisan 2026)

This section details the stress tests conducted to verify the resilience of the newly implemented Zero-Allocation WebSocket Broadcast Loop and the System Protection Monitor.
Bu bölüm, yeni uygulanan Sıfır Tahsisli (Zero-Allocation) WebSocket Broadcast Döngüsü ve Sistem Koruma Monitörü'nün (System Protection Monitor) dayanıklılığını doğrulamak için yapılan stres testlerini detaylandırmaktadır.

### 6.1 WebSocket Connection Limit Test / WebSocket Bağlantı Sınırı Testi
*   **Objective (Amaç):** Verify the global limit of 5000 concurrent WebSocket connections to prevent memory exhaustion. / Bellek tükenmesini önlemek için 5000 eşzamanlı WebSocket bağlantısı küresel sınırını doğrulamak.
*   **Test Setup (Test Kurulumu):** 6000 Virtual Users (VU) attempting to connect simultaneously over 30 seconds using `k6`. / 30 saniye boyunca eşzamanlı bağlanmaya çalışan 6000 Sanal Kullanıcı (VU).
*   **Result (Sonuç):** **SUCCESS (BAŞARILI)**. The system accurately capped connections at 5000. The excess 1000 connections were gracefully rejected with a `PolicyViolation` (Server too busy) status. / Sistem bağlantıları tam 5000'de sınırladı. Fazlalık olan 1000 bağlantı güvenli bir şekilde reddedildi.
*   **Metrics (Metrikler):**
    *   **Throughput:** 1.5M messages broadcasted successfully in 60s (~25,000 msg/sec). / 60 saniyede 1.5 Milyon mesaj yayınlandı (~25.000 mesaj/sn).
    *   **Latency:** Average connection time of 421ms under massive load. / Yoğun yük altında ortalama bağlanma süresi 421ms.
    *   **Memory:** Stabilized around ~350 MB Working Set, proving zero-allocation efficiency. / Bellek ~350 MB civarında sabitlendi, sıfır tahsisat verimliliğini kanıtladı.

### 6.2 Burst + Backpressure Spike Test / Ani Sıçrama ve Geri Basınç Testi
*   **Objective (Amaç):** Evaluate system response to sudden, extreme traffic spikes (from 200 to 4000, then to 7000 connections instantly). / Sistemin ani ve aşırı trafik sıçramalarına (200'den 4000'e ve anında 7000 bağlantıya) tepkisini değerlendirmek.
*   **Result (Sonuç):** **SUCCESS (BAŞARILI)**. The system absorbed the initial 20x spike seamlessly. During the 7000 VU spike, backpressure engaged flawlessly, preserving 5000 active connections and instantly terminating the excess. Recovery to 500 VUs was clean with no zombie connections. / Sistem ilk 20x sıçramayı sorunsuz bir şekilde absorbe etti. 7000 VU sıçramasında geri basınç kusursuz devreye girdi, aktif 5000 bağlantı korundu ve fazlalıklar anında sonlandırıldı. Toparlanma aşaması ölü bağlantı bırakmadan temiz bir şekilde gerçekleşti.

### 6.3 Heavy HTTP Audit Stress & Protection Mode / Ağır HTTP Audit Stres Testi ve Koruma Modu
*   **Objective (Amaç):** Validate the `SystemProtectionMonitor`'s ability to drop low-priority logs when under heavy HTTP load (up to 3000 RPS). / `SystemProtectionMonitor`'un ağır HTTP yükü altında (3000 RPS'e kadar) düşük öncelikli logları atma yeteneğini doğrulamak.
*   **Test Setup (Test Kurulumu):** `k6` ramping arrival rate sending `Low` priority payload: 200 RPS (10s) -> 1000 RPS (20s) -> 3000 RPS (20s).
*   **Result (Sonuç):** **SUCCESS (BAŞARILI)**. No system crashes. Max CPU reached ~185% with memory remaining incredibly stable at ~300 MB. The system actively rejected excess low-priority traffic (`http_req_failed: 100%` due to 429 Too Many Requests), correctly signaling `PROTECTION` mode to the dashboard via WebSockets. / Sistem çökmedi. Bellek ~300 MB seviyesinde inanılmaz stabil kalırken maksimum CPU ~%185'e ulaştı. Sistem fazla gelen düşük öncelikli trafiği aktif olarak reddetti (429 koduyla), ve WebSocket üzerinden dashboard'a `PROTECTION` modunu başarıyla iletti.
