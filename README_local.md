# Impo — Production-Ready .NET Microservices

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)]()
[![Tests](https://img.shields.io/badge/tests-27%20passing-success)]()
[![Build](https://img.shields.io/badge/build-passing-brightgreen)]()

**Gerçek dünyada kullanılabilir 4 microservice** - Event sourcing, Local-first CRM, LLM Gateway ve Static Analysis.

## 🚀 Hızlı Başlangıç (2 dakika)

```bash
# 1. Projeyi setup et
make install

# 2. Servisleri başlat
make start

# 3. Test verisi oluştur
make seed

# 4. Demo workflow'u çalıştır
make demo
```

**Hepsi bu!** Servislerin hazır ve kullanıma hazır.

## 📋 Ne Yapabilirsiniz?

### Makefile Komutları (Önerilen)

```bash
make help        # Tüm komutları göster
make status      # Servis durumlarını gör
make health      # Health check yap
make logs        # Logları izle
make test        # Testleri çalıştır
make swagger     # Swagger UI'ları aç
make urls        # Tüm URL'leri listele
```

### Manuel Kontrol

```bash
# Servis durumu
make status

# Çıktı:
#   🏟️  Spectator      [RUNNING] http://localhost:5185
#   👥  LocalCrm API   [RUNNING] http://localhost:5057
#   📱  LocalCrm App   [RUNNING] http://localhost:5209
#   🤖  PromptGateway  [RUNNING] http://localhost:5047
```

## 🎯 Kullanım Senaryoları

### 1. API Test Etmek İstiyorum

```bash
# Swagger UI'ları aç
make swagger

# Veya direkt:
open http://localhost:5185/swagger  # Spectator
open http://localhost:5057/swagger  # LocalCrm
open http://localhost:5047/swagger  # PromptGateway
```

**Swagger'dan direkt "Try it out" ile test edin!**

### 2. Gerçekçi Test Verisi İstiyorum

```bash
# 20 fake contact + 3 match oluşturur
make seed

# Sonuç:
# - LocalCrm: 20 realistic contact (Bogus ile)
# - Spectator: 3 match with 8-15 events each
```

### 3. Servislerin Nasıl Çalıştığını Görmek İstiyorum

```bash
# Interactive demo çalıştır
make demo

# Ne yapar?
# 1. Canlı maç oluşturur (gol, kart, vb.)
# 2. CRM'e contact ekler
# 3. PromptGateway'i test eder
# 4. Metrics toplar
# 5. Tüm sonuçları gösterir
```

### 4. Geliştirme Yapmak İstiyorum

```bash
# Bir servisi watch mode'da çalıştır (hot reload)
make dev-spectator    # Spectator'ı dev mode'da başlat
make dev-localcrm     # LocalCrm'i dev mode'da başlat
make dev-gateway      # PromptGateway'i dev mode'da başlat

# Değişiklikler otomatik reload olur
```

### 5. Kod Kalitesini Kontrol Etmek İstiyorum

```bash
# Archeologist ile static analysis
make analyze

# Bulacağı şeyler:
# - HttpClient leaks
# - Sync over async (.Result, .Wait())
# - SQL injection risks
# - Empty catch blocks
# - ve daha fazlası...
```

## 🏗️ Proje Mimarisi

### 🏟️ Spectator - Event Sourcing + Real-time

**Ne işe yarar?** Canlı maç skorları, event sourcing, time-travel replay

```bash
# Yeni event ekle
curl -X POST http://localhost:5185/api/matches/match1/events \
  -H "Content-Type: application/json" \
  -d '{"type":"goal","offsetSeconds":1350,"payloadJson":"{}"}'

# 45. dakikaya geri sar
curl http://localhost:5185/api/matches/match1/replay?at=2700

# SignalR ile real-time update
# UI: http://localhost:5185
```

**Teknolojiler:** SignalR, gRPC, Event Sourcing

### 👥 LocalCrm - Local-first Architecture

**Ne işe yarar?** Offline çalışan CRM, field-level conflict resolution

```bash
# Contact ekle (offline device simulation)
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{
    "contacts": [{
      "id": "...",
      "name": {"value": "John Doe", "updatedAt": "...", "deviceId": "mobile1"},
      "email": {"value": "john@example.com", "updatedAt": "...", "deviceId": "mobile1"}
    }]
  }'

# Blazor UI: http://localhost:5209
```

**Teknolojiler:** Blazor, SQLite, Field-level merging

### 🤖 PromptGateway - LLM Gateway

**Ne işe yarar?** PII masking, rate limiting, audit logging

```bash
# Budget kontrol
curl http://localhost:5047/admin/budget

# PII masking test (email, phone, SSN otomatik maskeler)
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "My email is john@example.com"}]
  }'
```

**Teknolojiler:** YARP, Regex-based PII detection, Token bucket

### 🔍 Archeologist - Static Analysis

**Ne işe yarar?** C# kod kalitesi kontrolü, anti-pattern tespiti

```bash
# Klasik kullanım
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy

# JSON output
dotnet run --project src/Archeologist/Archeologist.Cli -- src --json

# Sadece kritik bulgular
dotnet run --project src/Archeologist/Archeologist.Cli -- src --min-severity CRITICAL
```

**Teknolojiler:** Roslyn, Syntax tree analysis

## 🛠️ Geliştirici Araçları

### DataSeeder - Gerçekçi Test Verisi

```bash
make seed

# Oluşturur:
# - 20 realistic contacts (isim, email, telefon, şirket)
# - 3 match with random events
# - Hepsi Bogus library ile gerçekçi
```

### DemoRunner - Interactive Workflow

```bash
make demo

# Gösterir:
# - Cross-service communication
# - Real-time event streaming
# - CRM synchronization
# - API integration patterns
```

### Makefile - Standard Commands

```bash
make install     # Dependencies + setup
make build       # Build tüm projeler
make test        # Run tüm testler
make clean       # Temizle
make start       # Servisleri başlat
make stop        # Servisleri durdur
make restart     # Restart
make logs        # Log'ları izle
make health      # Health check
make status      # Detaylı durum
```

## 📊 Test & Kalite

```bash
# Tüm testleri çalıştır
make test

# Sonuç:
# ✅ Archeologist.Core.Tests:  11 tests PASS
# ✅ Spectator.Api.Tests:       8 tests PASS
# ✅ LocalCrm.Api.Tests:        8 tests PASS
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
#    TOPLAM:                   27 tests PASS
```

**Code Coverage:** ~70%  
**Build Time:** ~3 saniye  
**Startup Time:** ~10 saniye (tüm servisler)

## 🌐 Tüm URL'ler

| Servis          | UI                            | API Docs                          | Health          |
|-----------------|-------------------------------|-----------------------------------|-----------------|
| Dashboard       | `open dashboard.html`         | -                                 | -               |
| Spectator       | http://localhost:5185         | http://localhost:5185/swagger     | /health         |
| LocalCrm App    | http://localhost:5209         | -                                 | -               |
| LocalCrm API    | http://localhost:5057         | http://localhost:5057/swagger     | /health         |
| PromptGateway   | http://localhost:5047         | http://localhost:5047/swagger     | /health         |

## 🔧 Konfigürasyon

`.env.example` dosyasını `.env` olarak kopyalayın:

```bash
cp .env.example .env
```

**Önemli ayarlar:**
- `ENABLE_SWAGGER=true` - Swagger UI açık/kapalı
- `LOG_LEVEL=Information` - Log seviyesi
- `PROMPTGATEWAY_RATE_LIMIT_TOKENS=8000` - Saat başı limit

## 🚨 Sorun Giderme

### Port zaten kullanımda

```bash
make stop        # Tüm servisleri durdur
make start       # Yeniden başlat
```

### Servis başlamıyor

```bash
make logs        # Log'ları kontrol et
make health      # Health check yap
```

### Build hatası

```bash
make clean       # Temizle
make install     # Yeniden install
make build       # Build
```

## 📚 Dokümantasyon

- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Hızlı komut referansı
- **[API.md](API.md)** - Detaylı API dokümantasyonu
- **[TESTING.md](TESTING.md)** - Test kılavuzu
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - Katkı rehberi
- **[V4_RELEASE_NOTES.md](V4_RELEASE_NOTES.md)** - Son sürüm notları

## 💡 Best Practices

### CI/CD Pipeline

```bash
# Full CI pipeline
make ci

# Yapar:
# 1. make clean
# 2. make build
# 3. make test
```

### Development Workflow

```bash
# 1. Servisleri başlat
make start

# 2. Test verisi oluştur
make seed

# 3. Geliştirme yap (hot reload)
make dev-spectator

# 4. Test et
make test

# 5. Analyze code
make analyze
```

### Production Deployment

```bash
# 1. Environment setup
cp .env.example .env
# Edit .env for production

# 2. Build
dotnet build -c Release

# 3. Run
dotnet run --project src/Spectator/Spectator.Api -c Release
```

## 🎯 Öğrenme Yolu

1. **Hızlı başla:** `make start` → `make seed` → `make demo`
2. **API'leri keşfet:** `make swagger` ile interactive test
3. **Kod incele:** Her servisin `Program.cs` dosyasına bak
4. **Extend et:** Yeni endpoint ekle, test yaz
5. **Production'a hazırla:** Health checks, metrics, logging

## 🤝 Katkıda Bulunma

```bash
# 1. Fork + Clone
git clone your-fork

# 2. Branch oluştur
git checkout -b feature/amazing-feature

# 3. Geliştir ve test et
make test

# 4. Commit (conventional commits)
git commit -m "feat: add amazing feature"

# 5. Push ve PR aç
git push origin feature/amazing-feature
```

## 📈 Performans

| Metric              | Value        |
|---------------------|--------------|
| Cold start time     | ~10s         |
| Build time          | ~3s          |
| Test execution      | ~1s          |
| Memory usage (all)  | ~180MB       |
| Request latency     | <50ms        |

## 🏆 Production-Ready Features

✅ Health checks  
✅ Structured logging  
✅ Error handling  
✅ Input validation  
✅ Rate limiting  
✅ PII masking  
✅ Audit logging  
✅ Metrics collection  
✅ Unit tests (27 passing)  
✅ Integration ready  
✅ Swagger docs  
✅ Docker ready  

## 📦 Teknoloji Stack

- **.NET 10** - Latest framework
- **Roslyn** - C# compiler platform
- **SignalR** - Real-time web functionality
- **gRPC** - High-performance RPC
- **Blazor** - Web UI framework
- **SQLite** - Embedded database
- **YARP** - Reverse proxy
- **xUnit** - Testing framework
- **Bogus** - Fake data generation

---

**Version:** 4.1.0  
**License:** MIT  
**Maintainer:** Impo Team

💡 **Hızlı başlamak için:** `make start && make demo`
