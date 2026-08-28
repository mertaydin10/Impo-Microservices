# Impo v4.0 Release Notes

**Release Date:** August 28, 2026  
**Previous Version:** v3.0.0

## 🎯 Genel Bakış

v4.0, **kullanıcı deneyimini** ön plana çıkararak Impo projesini daha erişilebilir ve kullanışlı hale getiriyor. Bu sürüm, yeni başlayanlar için kolay başlangıç araçları, geliştiriciler için API dokümantasyonu ve sistem yöneticileri için merkezi yönetim dashboard'u sunuyor.

## ✨ Yeni Özellikler

### 1. 🎯 Dashboard (Landing Page)

**Dosya:** `dashboard.html`

Tüm servislerin durumunu tek bir sayfadan izleyebileceğiniz modern, responsive dashboard:

- **Gerçek Zamanlı Durum İzleme**: Her 10 saniyede bir servislerin health check'lerini otomatik kontrol eder
- **Hızlı Erişim**: Her servis için doğrudan link'ler (UI ve Health endpoints)
- **Görsel İstatistikler**: Toplam servis sayısı, çalışan servis sayısı, test sonuçları
- **Modern UI**: Dark tema, smooth animasyonlar, gradient efektler
- **Responsive Design**: Tüm cihazlarda mükemmel görünüm

**Kullanım:**
```bash
open dashboard.html
# veya
./start.sh  # Dashboard'u otomatik açar
```

**Özellikler:**
- 🏟️ **Spectator**: SignalR ve gRPC ile canlı maç skorları
- 👥 **LocalCrm**: Blazor + SQLite ile local-first CRM
- 🤖 **PromptGateway**: LLM Gateway (PII masking, rate limiting)
- 🔍 **Archeologist**: CLI-based kod analiz aracı

### 2. 🚀 Tek Komutla Başlatma - start.sh

**Dosya:** `start.sh`

Tüm servisleri tek bir komutla başlatan, akıllı yönetim script'i:

**Özellikler:**
- ✅ Otomatik dependency kontrolü (.NET SDK)
- ✅ Proje build'i
- ✅ Port kullanım kontrolü ve çakışma yönetimi
- ✅ Tüm servislerin paralel başlatılması
- ✅ Başarılı başlatma doğrulaması (30 saniyelik timeout)
- ✅ Canlı log izleme (`logs/` klasöründe)
- ✅ Graceful shutdown (Ctrl+C)
- ✅ Otomatik dashboard açılımı

**Kullanım:**
```bash
chmod +x start.sh
./start.sh
```

**Çıktı Örneği:**
```
╔═══════════════════════════════════════╗
║         🚀 IMPO STARTER v3.0         ║
║   Tüm Servisleri Başlatıyor...       ║
╚═══════════════════════════════════════╝

✓ .NET SDK bulundu: 10.0.100
📦 Proje build ediliyor...
✓ Build başarılı!

🔍 Portlar kontrol ediliyor...
✓ Tüm portlar müsait!

🚀 Servisler başlatılıyor...

🏟️  Spectator başlatılıyor (Port: 5185)...
✓ Spectator başarıyla başlatıldı!

👥 LocalCrm-API başlatılıyor (Port: 5057)...
✓ LocalCrm-API başarıyla başlatıldı!

📱 LocalCrm-App başlatılıyor (Port: 5209)...
✓ LocalCrm-App başarıyla başlatıldı!

🤖 PromptGateway başlatılıyor (Port: 5047)...
✓ PromptGateway başarıyla başlatıldı!

╔═══════════════════════════════════════╗
║     ✅ TÜM SERVİSLER BAŞLATILDI!     ║
╚═══════════════════════════════════════╝

📍 Erişim URL'leri:

  🏟️   Spectator:      http://localhost:5185
  👥  LocalCrm API:   http://localhost:5057
  📱  LocalCrm App:   http://localhost:5209
  🤖  PromptGateway:  http://localhost:5047

  🎯  Dashboard:      file:///path/to/dashboard.html

📊 Loglar: logs/ klasöründe
🛑 Durdurmak için: Ctrl+C
```

### 3. 📚 Swagger/OpenAPI Desteği

Tüm API'lere Swagger UI eklendi! Artık API'lerinizi tarayıcıdan keşfedebilir ve test edebilirsiniz.

#### Spectator API
- **Swagger UI**: `http://localhost:5185/swagger`
- **Endpoints:**
  - `POST /api/matches/{matchId}/events` - Yeni event ekle
  - `GET /api/matches/{matchId}/replay` - Belirli zamana geri sar
  - `GET /api/matches/{matchId}/events` - Tüm eventleri listele
  - `GET /api/matches` - Tüm maçları listele
  - `DELETE /api/matches/{matchId}` - Maç verisini temizle
  - `GET /api/metrics` - Metrics bilgisi

#### LocalCrm API
- **Swagger UI**: `http://localhost:5057/swagger`
- **Endpoints:**
  - `GET /contacts` - Tüm kişileri listele
  - `GET /contacts/{id}` - Tekil kişi getir
  - `POST /sync` - Kişileri senkronize et
  - `DELETE /contacts/{id}` - Kişi sil
  - `GET /stats` - İstatistikler

#### PromptGateway API
- **Swagger UI**: `http://localhost:5047/swagger`
- **Admin Endpoints:**
  - `GET /admin/audit` - Audit log'ları
  - `GET /admin/budget` - Budget durumu
  - `POST /admin/budget/reset/{key}` - Budget sıfırla
  - `GET /admin/routes` - Model route'ları
  - `POST /admin/routes` - Yeni route ekle

**Faydaları:**
- 📖 İnteraktif API dokümantasyonu
- 🧪 Tarayıcıdan doğrudan test edebilme
- 📝 Otomatik request/response şema gösterimi
- 🏷️ Endpoint'ler kategorize edilmiş (Tags ile)

## 🔧 Teknik İyileştirmeler

### Package Güncellemeleri

Tüm API projelerine `Swashbuckle.AspNetCore` v7.2.0 eklendi:

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
```

### Program.cs İyileştirmeleri

Her API'ye aşağıdaki kod bloğu eklendi:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "API Title", 
        Version = "v1",
        Description = "API Description"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Title v1");
    c.RoutePrefix = "swagger";
});
```

### Endpoint Metadata

Tüm API endpoint'lerine metadata eklendi:

```csharp
app.MapGet("/api/resource", () => Results.Ok())
.WithName("GetResource")      // Endpoint adı
.WithTags("Resources");        // Swagger kategorisi
```

## 📦 Yeni Dosyalar

1. **dashboard.html** - Merkezi yönetim ve izleme dashboard'u
2. **start.sh** - Tüm servisleri başlatan script (755 permissions ile)

## 🔄 Güncellemeler

### Tüm API Projeleri

- ✅ Swagger/OpenAPI desteği eklendi
- ✅ Endpoint metadata'ları eklendi
- ✅ İnteraktif API dokümantasyonu
- ✅ Health check endpoint'leri

### Altyapı

- ✅ Merkezi başlatma script'i
- ✅ Log yönetimi (`logs/` klasörü)
- ✅ PID tracking (her servis için)
- ✅ Graceful shutdown desteği

## 🎯 Kullanım Senaryoları

### Senaryo 1: Hızlı Başlangıç

Projeyi ilk kez indiren bir geliştirici:

```bash
# 1. Dashboard'u aç ve durumu gör
open dashboard.html

# 2. Tüm servisleri başlat
./start.sh

# 3. Swagger UI'dan API'leri keşfet
# Tarayıcıda: http://localhost:5185/swagger
```

### Senaryo 2: API Geliştirme

Yeni bir endpoint ekleyen geliştirici:

```bash
# 1. Servisleri başlat
./start.sh

# 2. Swagger UI'dan mevcut endpoint'leri incele
# http://localhost:5057/swagger

# 3. Yeni endpoint'i ekle ve test et
# 4. Swagger otomatik güncellenir!
```

### Senaryo 3: Production Monitoring

Sistem yöneticisi:

```bash
# 1. Dashboard'u aç
open dashboard.html

# 2. Tüm servislerin durumunu görsel olarak izle
# 3. Logları kontrol et: logs/*.log
# 4. Problem varsa tek servis'i restart et
```

## 📊 Metrikler & Performans

### Başlatma Süreleri

| Servis          | Başlatma Süresi | Port  |
|-----------------|-----------------|-------|
| Spectator       | ~2-3 saniye     | 5185  |
| LocalCrm API    | ~2-3 saniye     | 5057  |
| LocalCrm App    | ~3-4 saniye     | 5209  |
| PromptGateway   | ~2-3 saniye     | 5047  |

**Toplam:** ~10-13 saniye içinde tüm servisler hazır!

### Dashboard Performansı

- İlk yüklenme: ~100ms
- Health check döngüsü: 10 saniye
- Sayfa boyutu: ~20KB (gzip'siz)

## 🔐 Güvenlik

### start.sh Güvenliği

- ✅ Port çakışması kontrolü
- ✅ Kullanıcıdan onay isteme
- ✅ Graceful shutdown (SIGTERM/SIGINT)
- ✅ PID tracking (zombie process önleme)

### Dashboard Güvenliği

- ✅ Pure client-side (hassas veri yok)
- ✅ CORS politikası korunuyor
- ✅ Local-only erişim

## 🚀 Yükseltme Kılavuzu

v3.0'dan v4.0'a geçiş:

```bash
# 1. Yeni dosyaları çek
git pull

# 2. Bağımlılıkları güncelle
dotnet restore

# 3. Build et
dotnet build

# 4. Script'i executable yap
chmod +x start.sh

# 5. Yeni dashboard'u kullan
./start.sh
```

**Not:** Geriye uyumlu - mevcut tüm API'ler ve özellikler çalışmaya devam ediyor.

## 📝 Dokümantasyon Güncellemeleri

Gelecek güncellemelerde eklenecek:

- [ ] README.md güncellenmesi (quick start bölümü)
- [ ] API.md'ye Swagger link'leri eklenmesi
- [ ] TESTING.md'ye Swagger test senaryoları
- [ ] Docker Compose güncellemeleri

## 🙏 Teşekkürler

Bu sürüm, kullanıcı deneyimini ön plana çıkararak projeyi daha erişilebilir hale getirmeyi hedefliyor. Geri bildirimleriniz için teşekkürler!

## 🔗 Kaynaklar

- **Dashboard**: `dashboard.html`
- **Startup Script**: `start.sh`
- **Swagger UI (Spectator)**: `http://localhost:5185/swagger`
- **Swagger UI (LocalCrm)**: `http://localhost:5057/swagger`
- **Swagger UI (PromptGateway)**: `http://localhost:5047/swagger`

---

**Full Changelog:** v3.0.0...v4.0.0
