# CHANGELOG

## [2.0.0] - 2026-08-28

### Archeologist İyileştirmeleri
- ✨ JSON output desteği (`--json` flag)
- ✨ Severity filtreleme (`--min-severity` option)
- 🎯 Yeni pattern detection:
  - SQL injection risk
  - Boş catch blokları
  - Döngüde string concatenation
  - Thread.Sleep kullanımı
- 📊 Severity seviyeleri (LOW, MEDIUM, HIGH, CRITICAL)
- 📈 Gelişmiş rapor formatı

### Spectator İyileştirmeleri
- 🎨 Tamamen yenilenmiş UI
  - Animasyonlar ve transitions
  - Live status badge
  - Event badges (goal, card, sub)
  - Real-time istatistikler
- ⚡ Yeni API endpoints:
  - `GET /api/matches` - Tüm maçları listele
  - `DELETE /api/matches/{id}` - Maç temizle
  - `GET /api/metrics` - Sistem metrikleri
  - `GET /health` - Health check
- 📊 MetricsCollector: Event tracking, uptime, aktif maçlar
- 🔄 Çoklu maç desteği
- 🎯 Gelişmiş event store

### LocalCrm İyileştirmeleri
- ✅ Validasyon sistemi:
  - Email format kontrolü
  - Telefon uzunluk kontrolü
  - Zorunlu alan kontrolü
- 🎯 Yeni API endpoints:
  - `GET /contacts/{id}` - Tek contact getir
  - `DELETE /contacts/{id}` - Contact sil
  - `GET /stats` - İstatistikler
  - `GET /health` - Health check
- 🔧 Gelişmiş hata yönetimi
- 📊 Database timestamp tracking
- ⚠️ Detaylı error messages

### PromptGateway İyileştirmeleri
- 🔒 Gelişmiş PII maskeleme:
  - Email addresses
  - Kredi kartı numaraları
  - Social Security Numbers
  - Telefon numaraları
  - IP adresleri
  - Credentials (password, api_key, token)
- ⚡ Token bucket rate limiting (saatlik reset)
- 🎯 Dinamik model routing
- 🔧 Yeni admin endpoints:
  - `POST /admin/budget/reset/{key}` - Budget sıfırla
  - `GET /admin/routes` - Model route'ları görüntüle
  - `POST /admin/routes` - Yeni route ekle
  - `GET /health` - Health check
- 📊 Gelişmiş audit logging

### Genel İyileştirmeler
- ✅ Health checks tüm servislerde
- 📊 Metrics ve monitoring
- 🔧 Production-ready error handling
- 📝 Kapsamlı README güncellemesi
- 🎯 API documentation

## [1.0.0] - Initial Release
- ✨ Archeologist - Kod analiz CLI
- ✨ Spectator - Event sourcing + SignalR
- ✨ LocalCrm - Local-first CRM
- ✨ PromptGateway - LLM gateway
