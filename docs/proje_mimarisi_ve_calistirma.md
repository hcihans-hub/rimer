# 🏗️ RIMER Proje Mimarisi ve Çalıştırma Rehberi

Bu belge, Rektörlük İletişim Merkezi (RIMER) projesinin teknik yapısını, katmanlı mimarisini ve sistemi nasıl ayağa kaldıracağınızı açıklar.

---

## 📌 Proje Özeti
RIMER, üniversite yönetimi ile paydaşlar (öğrenci, personel, dış kullanıcılar) arasındaki iletişimi dijitalleştiren, yüksek performanslı ve güvenli bir ticket (destek kaydı) yönetim sistemidir.

---

## 🏛️ Teknik Mimari (Clean Architecture)

Proje, bağımlılıkların içeriye doğru aktığı ve çekirdek iş mantığının (Domain) merkezde olduğu **Clean Architecture** prensiplerine göre yapılandırılmıştır:

### 1. RimerApi.Domain
- **Amacı:** Sistemin kalbidir. Hiçbir dış kütüphaneye veya katmana bağımlı değildir.
- **İçerik:** Temel varlıklar (Entities), Değer Nesneleri (Value Objects), Enumlar ve Domain kuralları.

### 2. RimerApi.Application
- **Amacı:** İş mantığı (Business Logic) ve kullanım senaryolarını (Use Cases) yönetir.
- **İçerik:** Servis arayüzleri, DTO'lar, AutoMapper profilleri ve validasyon kuralları.

### 3. RimerApi.Infrastructure
- **Amacı:** Sistemin dış dünyayla etkileşimini sağlar.
- **İçerik:** 
  - **Data:** Entity Framework Core (SQL Server) ve Migrations.
  - **Identity:** JWT tabanlı kimlik doğrulama ve yetkilendirme servisleri.
  - **WebSockets:** SignalR ile anlık bildirim hub'ları.
  - **Background Jobs:** Hangfire ile arka plan işleme (Audit log arşivleme vb.).

### 4. RimerApi.API
- **Amacı:** Sistemin giriş kapısıdır.
- **İçerik:** RESTful Controller'lar, Swagger dokümantasyonu, Middleware'ler ve konfigürasyon.

---

## 🛠️ Kullanılan Teknolojiler

### Backend
- **ASP.NET Core 10:** Modern ve hızlı web API framework.
- **EF Core:** Veritabanı yönetim katmanı.
- **SignalR:** Gerçek zamanlı çift taraflı iletişim.
- **Hangfire:** Arka plan iş yönetimi ve zamanlanmış görevler.
- **Serilog:** Yapılandırılmış log yönetimi.
- **OpenTelemetry:** Metrik ve izleme (Observability).

### Frontend
- **Vue.js 3:** Reactive ve modern kullanıcı arayüzü framework'ü.
- **Vite:** Hızlı geliştirme ve build aracı.
- **Tailwind CSS:** Utility-first stil kütüphanesi.
- **ECharts:** Veri görselleştirme ve analiz grafikleri.

---

## ⚙️ Sistemi Çalıştırma Adımları

### Ön Gereksinimler
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js (v20+)](https://nodejs.org/)
- SQL Server (Veya Docker üzerinde SQL Express)

### 1. Backend Hazırlığı
Terminalde projenin ana dizinine gidin:

```powershell
# API dizinine git
cd src/RimerApi.API

# Bağımlılıkları yükle ve projeyi çalıştır
dotnet run
```
API varsayılan olarak `http://localhost:5000` adresinden yayın yapar. Swagger arayüzüne `http://localhost:5000/swagger` adresinden erişebilirsiniz.

### 2. Frontend Hazırlığı
Yeni bir terminal açın ve dashboard dizinine gidin:

```powershell
# Dashboard dizinine git
cd rimer-dashboard

# Bağımlılıkları yükle
npm install

# Geliştirme sunucusunu başlat
npm run dev
```
Uygulama `http://localhost:5173/` adresinde çalışmaya başlayacaktır.

---

## 🛡️ Sistem Koruması ve Güvenlik
- **Adaptive Load Shedding:** Sistem CPU veya Bellek kullanımı kritik seviyeye ulaştığında otomatik olarak yükü hafifletir.
- **JWT Auth:** Güvenli kimlik doğrulama ve rol tabanlı yetkilendirme (Admin, Staff, Student).
- **Audit Logging:** Tüm kritik işlemler asenkron olarak kaydedilir ve periyodik olarak arşivlenir.
