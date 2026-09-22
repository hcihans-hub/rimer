# Rimer API Yüksek Kapasite Stres Testi ve Optimizasyon

**Tarih:** 21 Nisan 2026  
**Versiyon:** 3.0 
**Yazar:** Hasan Cihan Şenküçük Software Developer 
**Sınıflandırma:** Teknik / Altyapı Analizi

---

## 1. Özet
Bu döküman, Rimer API sisteminin performans evriminin kapsamlı bir teknik analizini sunar. Başlangıçta 200-400 RPS (Saniye Başına İstek) eşiğinde tespit edilen darboğazların ardından, bir dizi mimari iyileştirme ve düşük seviyeli SQL optimizasyonu uygulanmıştır.

Yapılan son doğrulamalar, sistemin artık saniyede **400 isteği (RPS)** %100 kullanılabilirlik, sıfır veri kaybı ve sürdürülebilir kaynak kullanımı ile karşıladığını kanıtlamaktadır. Bu sonuçlar, projenin yüksek trafikli kurumsal ortamlar için "Üretime Hazır" (Production-Ready) olduğunu teyit eder.

---

## 2. Sorun Analizi: 200-400 RPS Darboğazı v2 ' dökümanı 

### 2.1 Gözlemlenen Davranışlar
Yük kademeli olarak artırıldığında, sistem 200-300 RPS eşiğini geçerken ciddi bir performans kaybı yaşamıştır:
*   **Gecikme Sıçramaları:** Arka plandaki audit log işleme süresi (latency) logaritmik olarak artmıştır.
*   **SQL Zaman Aşımı:** `Microsoft.Data.SqlClient.SqlException` hataları arka plan worker'larında sistem durmalarına yol açmıştır.
*   **Log Şişmesi:** Gizli hatalar, uygulama hata loglarının günde **235 MB** büyüklüğüne ulaşmasına neden olarak gerçek sistem kapasitesini maskelemiştir.

### 2.2 Kök Neden Analizi (RCA)
1.  **Kilitleme Çatışması (Lock Escalation):** `AuditLogQueueWorker`, yüksek hızlı `INSERT` işlemleriyle aynı döngü içinde "Takılı Kalan Kayıtları Kurtarma" (Full table scan `UPDATE`) işlemini yapıyordu. Bu durum tablo kilitlenmelerine yol açarak ana yazma hattını tıkamıştır.
2.  **Geçersiz Skaler İfade:** SQL TVP sorgusundaki `NEWSEQUENTIALID()` kullanım hatası, kayıtların veritabanına işlenmesini engellemiş ve sonsuz bir "tekrar dene-hata al" döngüsü oluşturmuştur.
3.  **Rate-Limit Doygunluğu:** Global rate-limiter, gerçek sistem kapasitesinin çok altında bir eşikte (10k/dk) devreye girerek test ölçümlerini yanıltmıştır.

---

## 3. Çözümler

### 3.1 Mimari Ayrıştırma (Öncelik 1)
Kurtarma mantığı, yüksek hızlı worker'dan tamamen çıkarılmıştır. Özel bir **`AuditLogRecoveryJob`** geliştirilerek bağımsız bir Hangfire görevi olarak tanımlanmıştır.
*   **Etki:** Ana `AuditLogs` tablosu üzerinde yazma işlemi sırasında artık hiçbir bakım/güncelleme çatışması yaşanmamaktadır.

### 3.2 SQL Veri İşleme Optimizasyonu
TVP (Table-Valued Parameter) veri işleme süreci yeniden yapılandırıldı:
*   **Otomatik Kimlik Mantığı:** Uygulama katmanındaki manuel ID üretimi kaldırıldı. Veritabanı, `NEWSEQUENTIALID()` kısıtlamasıyla ID atama işlemini kendisi yapacak şekilde optimize edildi; bu, istemci tarafı GUID üretiminden çok daha hızlıdır.
*   **Batch Hizalaması:** SQL'e yapılan toplu gönderimlerin boyutları, tablo kilitlenmelerini tetiklemeyecek en optimize seviyeye ayarlandı.

### 3.3 Veri Saklama ve Arşivleme ("Temiz DB" Politikası)
Zamanla oluşabilecek performans kayıplarını önlemek için otomatik **"Sıcak-Ilık-Soğuk"** arşivleme stratejisi devreye alınmıştır:
*   **Canlı Veri:** SQL veritabanı en güncel **3 günlük** logu tutar.
*   **Arşivleme:** 3-7 günlük kayıtlar GZip ile sıkıştırılıp dosya sistemine taşınır.
*   **Temizleme:** 7 günü geçen tüm kayıtlar SQL Server'dan tamamen silinerek tablo boyutunun öngörülebilir kalması sağlanır.

---

## 4. Final Doğrulama Sonuçları (400 RPS)

| Metrik | Ölçülen Değer | Sonuç |
| :--- | :--- | :--- |
| **İşleme Kapasitesi (Peak)** | 400 RPS | **GEÇTİ** |
| **Toplam Test Hacmi** | ~24,000 istek  | **Doğrulandı** |
| **Kaydedilen Audit Log** | **67,314 Kayıt** | **%100 Başarı** |
| **Kritik Hata Sayısı** | 0 | **GEÇTİ** |
| **SQL Zaman Aşımı** | 0 | **GEÇTİ** |
| **Ortalama API Gecikmesi** | < 150ms | **Son Derece Akıcı** |
| **Hata Logu Boyutu** | 9.8 MB (Stres Altında) | **KONTROLLÜ** |

---

## 5. Karar
Rimer API sistemi, I/O darboğazı yaşanan bir yapıdan, **doğrusal ölçeklenebilir (linearly scalable)** bir mimariye başarıyla geçiş yapmıştır. Uygulanan düzeltmeler veritabanı katmanındaki temel çekişme sorunlarını çözmüştür.

**Üretim Ortamı İçin Tavsiyeler:**
*   Prometheus üzerinden `hangfire_queue_depth_default` metriğini izleyin; derinlik 5 dakikadan uzun süre 10.000'i aşarsa alarm üretin.
*   Sistem şu an için her instance başına **500 RPS** kapasiteyle çalışmaya uygun bulunmuştur.

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
### 40 yaşından sonra da pek bir performans gösteremeyebiliriz değil mi :D     
