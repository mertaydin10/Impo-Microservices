# 🚀 Impo v4.0 - Hızlı Başvuru Kartı

## ⚡ Hızlı Başlangıç

```bash
# 1. Tüm servisleri başlat
./start.sh

# 2. Dashboard'u aç
open dashboard.html
```

## 🎯 Servis URL'leri

| Servis          | UI/App                        | Swagger API                       | Health          |
|-----------------|-------------------------------|-----------------------------------|-----------------|
| **Spectator**   | http://localhost:5185         | http://localhost:5185/swagger     | /health         |
| **LocalCrm**    | http://localhost:5209         | http://localhost:5057/swagger     | /health         |
| **PromptGW**    | http://localhost:5047         | http://localhost:5047/swagger     | /health         |
| **Dashboard**   | `open dashboard.html`         | -                                 | -               |

## 📚 API Hızlı Referans

### Spectator API (Port 5185)

```bash
# Yeni event ekle
curl -X POST http://localhost:5185/api/matches/match1/events \
  -H "Content-Type: application/json" \
  -d '{"type":"goal","offsetSeconds":45,"payloadJson":"{}"}'

# Maç replay
curl http://localhost:5185/api/matches/match1/replay?at=120

# Tüm maçlar
curl http://localhost:5185/api/matches

# Metrics
curl http://localhost:5185/api/metrics
```

### LocalCrm API (Port 5057)

```bash
# Tüm kişiler
curl http://localhost:5057/contacts

# Tek kişi
curl http://localhost:5057/contacts/{guid}

# Sync
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{"contacts":[...]}'

# İstatistikler
curl http://localhost:5057/stats
```

### PromptGateway API (Port 5047)

```bash
# Audit log
curl http://localhost:5047/admin/audit

# Budget durumu
curl http://localhost:5047/admin/budget

# Budget reset
curl -X POST http://localhost:5047/admin/budget/reset/user123

# Model routes
curl http://localhost:5047/admin/routes
```

## 🔍 Archeologist CLI

```bash
# Basit tarama
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy

# JSON output
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy --json

# Sadece kritik bulgular
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy --min-severity CRITICAL
```

## 🛠️ Geliştirme Komutları

```bash
# Build
dotnet build

# Test
dotnet test

# Tek proje çalıştır
dotnet run --project src/Spectator/Spectator.Api

# Watch mode (hot reload)
dotnet watch --project src/LocalCrm/LocalCrm.App
```

## 📊 Log & Monitoring

```bash
# Tüm log'ları izle
tail -f logs/*.log

# Tek servis log'u
tail -f logs/Spectator.log

# Process ID'leri gör
cat logs/*.pid

# Servis durumlarını kontrol et
curl http://localhost:5185/health
curl http://localhost:5057/health
curl http://localhost:5047/health
```

## 🚨 Sorun Giderme

### Port zaten kullanımda

```bash
# Port'u kullanan process'i bul
lsof -ti:5185

# Process'i durdur
lsof -ti:5185 | xargs kill -9

# Veya start.sh otomatik soracak:
./start.sh  # Portlar kullanımdaysa otomatik sorar
```

### Servis başlamıyor

```bash
# Log'u kontrol et
cat logs/Spectator.log

# Manuel başlat ve hata mesajını gör
dotnet run --project src/Spectator/Spectator.Api
```

### Build hatası

```bash
# Temiz build
dotnet clean
dotnet restore
dotnet build
```

## 🎯 Dashboard Özellikleri

- **Gerçek Zamanlı Durum**: Her 10 saniyede otomatik güncelleme
- **Hızlı Erişim**: Her servis için direkt link'ler
- **İstatistikler**: Servis sayısı, test sonuçları
- **Modern UI**: Dark tema, animasyonlar

## 📦 Proje Yapısı

```
impo/
├── dashboard.html          # 🎯 Merkezi dashboard
├── start.sh                # 🚀 Başlatma script'i
├── src/
│   ├── Archeologist/       # 🔍 Kod analiz CLI
│   ├── Spectator/          # 🏟️  Event sourcing API
│   ├── LocalCrm/           # 👥 Local-first CRM
│   └── PromptGateway/      # 🤖 LLM Gateway
└── logs/                   # 📊 Servis log'ları
```

## 🎓 Öğren & Keşfet

1. **Dashboard'u Aç**: `open dashboard.html`
2. **Servisleri Başlat**: `./start.sh`
3. **Swagger UI'ı Keşfet**: http://localhost:5185/swagger
4. **API'leri Dene**: Swagger UI'dan "Try it out"
5. **Log'ları İzle**: `tail -f logs/*.log`

## 🔗 Dokümantasyon

- **[README.md](README.md)** - Genel bakış
- **[V4_RELEASE_NOTES.md](V4_RELEASE_NOTES.md)** - v4.0 detayları
- **[API.md](API.md)** - Detaylı API docs
- **[TESTING.md](TESTING.md)** - Test kılavuzu
- **[V4_DEVELOPMENT_SUMMARY.md](V4_DEVELOPMENT_SUMMARY.md)** - Kapsamlı geliştirme özeti

## 💡 Pro Tips

```bash
# Tek komutla health check
for port in 5185 5057 5047; do curl -f http://localhost:$port/health || echo "Port $port DOWN"; done

# JSON formatında tüm maçları al
curl -s http://localhost:5185/api/matches | jq '.'

# Dashboard'u startup'a ekle (macOS)
echo "open $(pwd)/dashboard.html" >> ~/.zshrc
```

## 🎉 Temel Workflow

```
1. ./start.sh                    → Tüm servisleri başlat
2. open dashboard.html           → Durumu görsel olarak izle
3. open http://localhost:5185/swagger  → API'leri keşfet
4. Test et, geliştir, commit et
5. Ctrl+C                        → Tüm servisleri durdur
```

---

**Version:** 4.0.0  
**Toplam Test:** 27 passing ✅  
**Başlatma Süresi:** ~10 saniye  
**Dashboard Yenileme:** 10 saniye  

💡 **Yardıma mı ihtiyacınız var?** Dokümantasyonu okuyun veya Swagger UI'ı kullanın!
