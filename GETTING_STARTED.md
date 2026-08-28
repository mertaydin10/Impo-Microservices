# 🚀 Getting Started with Impo

Bu rehber sizi **5 dakikada** Impo projesiyle çalışır hale getirecek.

## ✅ Gereksinimler

- **.NET 10 SDK** - [İndir](https://dotnet.microsoft.com/download)
- **Git** - Version control
- **Terminal** - bash/zsh (macOS/Linux) veya PowerShell (Windows)
- **Make** - `brew install make` (macOS) veya Windows için [GnuWin32](http://gnuwin32.sourceforge.net/packages/make.htm)

### Kontrol Et

```bash
dotnet --version  # 10.0.x olmalı
git --version
make --version
```

## 📥 1. Projeyi İndir

```bash
# Clone the repo
git clone <repo-url>
cd impo

# Setup yap
make install
```

**Ne yapar `make install`?**
- Tüm NuGet paketlerini indirir
- `start.sh` executable yapar
- `logs/` ve `data/` klasörlerini oluşturur

## 🏗️ 2. Build Et

```bash
make build
```

**Sonuç:**
```
✓ Build complete!
  - 10 projects compiled
  - 0 warnings
  - 0 errors
```

## ✅ 3. Testleri Çalıştır

```bash
make test
```

**Göreceksiniz:**
```
✓ All tests passed!
  - Archeologist: 11 tests ✓
  - Spectator: 8 tests ✓  
  - LocalCrm: 8 tests ✓
```

## 🚀 4. Servisleri Başlat

```bash
make start
```

**10 saniye sonra:**
```
╔═══════════════════════════════════════╗
║     ✅ TÜM SERVİSLER BAŞLATILDI!     ║
╚═══════════════════════════════════════╝

📍 Erişim URL'leri:
  🏟️   Spectator:      http://localhost:5185
  👥  LocalCrm API:   http://localhost:5057
  📱  LocalCrm App:   http://localhost:5209
  🤖  PromptGateway:  http://localhost:5047
```

### Alternatif: Manuel Başlatma

```bash
# Her servisi ayrı terminal'de
dotnet run --project src/Spectator/Spectator.Api
dotnet run --project src/LocalCrm/LocalCrm.Api
dotnet run --project src/LocalCrm/LocalCrm.App
dotnet run --project src/PromptGateway/PromptGateway.Api
```

## 🌱 5. Test Verisi Oluştur

```bash
make seed
```

**Sonuç:**
```
🌱 Impo Data Seeder - Generating realistic test data...

📡 Checking service availability...
  ✓ LocalCrm is online
  ✓ Spectator is online
  ✓ PromptGateway is online

👥 Seeding LocalCrm contacts...
  ✓ Created 20 contacts

🏟️  Seeding Spectator match events...
  ✓ Created 12 events for match-real-vs-barca
  ✓ Created 10 events for match-chelsea-vs-bayern
  ✓ Created 14 events for match-psg-vs-milan

✅ Data seeding complete!
```

## 🎬 6. Demo'yu Çalıştır

```bash
make demo
```

**Göreceksiniz:**
```
🎬 Impo Demo Runner - Interactive workflow demonstration

═══════════════════════════════════════════════════════════
  DEMO: Cross-Service Integration Workflow
═══════════════════════════════════════════════════════════

📋 Scenario: Real-time match tracking with CRM integration

🏟️  SPECTATOR: Creating live match...
  ▸ Creating kick-off event... ✓
  ▸ Scoring a goal at 23'... ✓
  ▸ Yellow card at 45'... ✓

[... tüm workflow'u gösterir ...]

✅ Demo Complete!
```

## 🎯 7. İlk API Çağrısı

### Swagger UI (Kolay Yol)

```bash
make swagger  # Tüm Swagger UI'ları açar
```

Veya manuel:
- http://localhost:5185/swagger - Spectator
- http://localhost:5057/swagger - LocalCrm
- http://localhost:5047/swagger - PromptGateway

**"Try it out" butonuna tıklayıp test edin!**

### cURL (Terminal)

```bash
# Yeni maç eventi ekle
curl -X POST http://localhost:5185/api/matches/my-match/events \
  -H "Content-Type: application/json" \
  -d '{
    "type": "goal",
    "offsetSeconds": 1350,
    "payloadJson": "{\"team\": \"Home\", \"player\": \"Messi\"}"
  }'

# Sonuç:
# {"evt":{...},"snap":{...}}
```

```bash
# Tüm contact'ları getir
curl http://localhost:5057/contacts

# Sonuç:
# [{"id":"...","name":{...},"email":{...}}]
```

## 📊 8. Dashboard'u Aç

```bash
make dashboard
```

Veya manuel:
```bash
open dashboard.html  # macOS
xdg-open dashboard.html  # Linux
start dashboard.html  # Windows
```

**Dashboard özellikleri:**
- ✅ Gerçek zamanlı servis durumu
- ✅ Her 10 saniyede otomatik güncelleme
- ✅ Tüm servislere hızlı link'ler
- ✅ İstatistikler ve metrikler

## 🔍 9. Durumu Kontrol Et

```bash
make status
```

**Çıktı:**
```
Service Status:

  🏟️  Spectator      [RUNNING] http://localhost:5185
  👥 LocalCrm API   [RUNNING] http://localhost:5057
  📱 LocalCrm App   [RUNNING] http://localhost:5209
  🤖 PromptGateway  [RUNNING] http://localhost:5047
```

### Health Check

```bash
make health
```

**Çıktı:**
```
Checking service health...
Spectator (5185): ✓ OK
LocalCrm API (5057): ✓ OK
PromptGateway (5047): ✓ OK
```

## 🛑 10. Servisleri Durdur

```bash
make stop
```

**Sonuç:**
```
Stopping services...
✓ Services stopped!
```

---

## 🎓 Sırada Ne Var?

### Öğrenme Yolu

1. **API'leri Keşfet**
   ```bash
   make swagger
   ```
   Her endpoint'i Swagger UI'dan test edin.

2. **Kod İncele**
   ```bash
   # Basit ve okunabilir kod
   cat src/Spectator/Spectator.Api/Program.cs
   cat src/LocalCrm/LocalCrm.Api/Program.cs
   ```

3. **Testlere Bak**
   ```bash
   # Unit test örnekleri
   cat src/Spectator/Spectator.Api.Tests/SpectatorTests.cs
   ```

4. **Yeni Feature Ekle**
   ```bash
   # Watch mode'da geliştir (hot reload)
   make dev-spectator
   ```

### Kullanım Senaryoları

#### Senaryo 1: Real-time Match Tracker

```bash
# 1. Match UI'yi aç
open http://localhost:5185

# 2. Terminal'den event gönder
curl -X POST http://localhost:5185/api/matches/live/events \
  -H "Content-Type: application/json" \
  -d '{"type":"goal","offsetSeconds":600,"payloadJson":"{}"}'

# 3. UI'da anlık güncellemeyi gör!
```

#### Senaryo 2: Offline CRM Sync

```bash
# 1. Blazor App'i aç
open http://localhost:5209

# 2. Offline contact ekle (UI'dan)

# 3. API'yi durdur
pkill -f LocalCrm.Api

# 4. Daha fazla contact ekle (offline çalışır!)

# 5. API'yi başlat, sync olsun
dotnet run --project src/LocalCrm/LocalCrm.Api
```

#### Senaryo 3: PII Masking Test

```bash
# Hassas veri içeren prompt gönder
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{
      "role": "user",
      "content": "My email is john@secret.com and my SSN is 123-45-6789"
    }]
  }'

# Email ve SSN otomatik maskeli gelecek!
```

## 🚨 Sorun Giderme

### "Port already in use"

```bash
make stop        # Tüm servisleri durdur
make start       # Yeniden başlat
```

### "dotnet: command not found"

.NET SDK yüklü değil:
```bash
# macOS
brew install dotnet

# veya: https://dotnet.microsoft.com/download
```

### "make: command not found"

Make yüklü değil:
```bash
# macOS
brew install make

# Linux
sudo apt install make

# Windows: GnuWin32 Make kullanın
```

### Servis başlamıyor

```bash
# Log'ları kontrol et
make logs

# veya
tail -f logs/Spectator.log
```

### Build hatası

```bash
# Temizle ve yeniden
make clean
make install
make build
```

## 💡 Faydalı Komutlar

```bash
make help        # Tüm komutları göster
make status      # Servis durumları
make logs        # Canlı log izle
make urls        # Tüm URL'leri listele
make ci          # Full CI pipeline (clean, build, test)
make analyze     # Kod analizi (Archeologist)
```

## 📚 Dokümantasyon

- **[README.md](README.md)** - Ana dokümantasyon
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Hızlı referans
- **[API.md](API.md)** - Detaylı API docs
- **[TESTING.md](TESTING.md)** - Test guide

## 🎉 Tebrikler!

Artık Impo projesini kullanmaya hazırsınız!

**İlk adım:** `make start && make demo`

Sorular için: Issues açın veya dokümantasyonu okuyun.
