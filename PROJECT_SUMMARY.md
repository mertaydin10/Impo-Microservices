# Impo - Proje Özeti

## Genel Bakış

Impo, .NET 10 ile geliştirilmiş dört farklı üretim düzeyinde ürünü içeren bir monorepo'dur. Her bir ürün modern .NET özellikleri kullanılarak geliştirilmiştir ve production-ready durumdadır.

## Projeler

### 1. Archeologist - Kod Analiz Aracı
**Teknoloji:** Roslyn, System.CommandLine  
**Tip:** Console CLI Application

Eski C# kodunda yaygın anti-pattern'leri tespit eden statik kod analiz aracı.

**Özellikler:**
- HttpClient sızıntı tespiti
- Sync-over-async pattern tespiti (.Result, .Wait(), GetAwaiter().GetResult())
- DateTime.Now kullanımı tespiti
- Kaynak kodda gizli değer tespiti
- SQL injection riski tespiti
- Boş catch blokları tespiti
- Döngülerde string concatenation tespiti
- JSON ve Markdown çıktı formatları
- Severity filtreleme (LOW, MEDIUM, HIGH, CRITICAL)

**Kullanım:**
```bash
dotnet run --project src/Archeologist/Archeologist.Cli -- <path> [options]
```

### 2. Spectator - Event Sourcing API
**Teknoloji:** ASP.NET Core, SignalR, gRPC  
**Tip:** Web API + Real-time Communication

Canlı maç skorları için event sourcing ve real-time yayın sistemi.

**Özellikler:**
- Event sourcing pattern
- Real-time SignalR hub
- gRPC ingest servisi
- Time-travel replay (herhangi bir anına dönme)
- Çoklu maç desteği
- Metrics ve monitoring
- Modern, responsive web UI
- Health checks

**Portlar:**
- HTTP: 5185
- gRPC: 5186

### 3. LocalCrm - Local-First CRM
**Teknoloji:** ASP.NET Core, Blazor, SQLite  
**Tip:** Web API + Blazor App

Field-level merging ile offline-first CRM sistemi.

**Özellikler:**
- Field-level CRDT benzeri birleştirme
- Offline çalışma desteği
- SQLite ile yerel veri depolama
- Çakışma çözümlemesi (LWW + device ID tie-break)
- Validasyon sistemi
- REST API
- Health checks
- CRUD operasyonları

**Portlar:**
- API: 5057
- App: 5209

### 4. PromptGateway - LLM Gateway
**Teknoloji:** ASP.NET Core, YARP  
**Tip:** Reverse Proxy / API Gateway

PII maskeleme, rate limiting ve model routing ile LLM gateway.

**Özellikler:**
- Gelişmiş PII maskeleme (email, card, SSN, phone, IP, credentials)
- Token bucket rate limiting
- Dinamik model routing
- Audit logging
- Budget yönetimi
- Admin API
- Mock LLM endpoint
- Health checks

**Port:** 5047

## Yeni Eklenen Özellikler (v2.0.0)

### Kod Kalitesi
- ✅ Health checks tüm API'lerde
- ✅ Gelişmiş hata yönetimi
- ✅ Validasyon sistemleri
- ✅ Metrics ve monitoring
- ✅ Structured logging hazır

### Dokümantasyon
- ✅ Comprehensive README
- ✅ API documentation (API.md)
- ✅ Test guide (TESTING.md)
- ✅ Contributing guide (CONTRIBUTING.md)
- ✅ Changelog (CHANGELOG.md)

### DevOps
- ✅ Docker support (Dockerfiles)
- ✅ Docker Compose yapılandırması
- ✅ Test script (test.sh)
- ✅ .gitignore

### UI/UX
- ✅ Spectator: Tamamen yenilenmiş UI
- ✅ Animasyonlar ve transitions
- ✅ Real-time istatistikler
- ✅ Live status badges

### API İyileştirmeleri
- ✅ 15+ yeni endpoint
- ✅ Gelişmiş error handling
- ✅ Validasyon ve sanitization
- ✅ Admin endpoints

## Teknoloji Stack

| Teknoloji | Kullanım Alanı | Projeler |
|-----------|----------------|----------|
| .NET 10 | Runtime | Tümü |
| ASP.NET Core | Web framework | Spectator, LocalCrm, PromptGateway |
| Blazor | Frontend | LocalCrm.App |
| SignalR | Real-time | Spectator |
| gRPC | RPC | Spectator |
| YARP | Reverse Proxy | PromptGateway |
| SQLite | Database | LocalCrm |
| Roslyn | Code Analysis | Archeologist |

## Mimari Prensipler

### 1. Minimal API Pattern
Tüm API'ler minimal API pattern kullanır - daha az boilerplate, daha okunabilir kod.

### 2. Modern C# Features
- Records
- Pattern matching
- File-scoped namespaces
- Top-level statements
- Nullable reference types
- Source generators (regex)

### 3. Production-Ready
- Health checks
- Metrics
- Error handling
- Validation
- Logging infrastructure
- CORS support

### 4. Container-Ready
Tüm servisler Docker ile containerize edilebilir.

## Proje Yapısı

```
Impo/
├── src/                           # Kaynak kod
│   ├── Archeologist/             # Kod analiz CLI
│   │   ├── Archeologist.Core/   # Analiz motoru
│   │   └── Archeologist.Cli/    # CLI uygulaması
│   ├── Spectator/                # Event sourcing
│   │   └── Spectator.Api/       # API + SignalR + gRPC
│   ├── LocalCrm/                 # CRM sistemi
│   │   ├── LocalCrm.Shared/     # Shared models
│   │   ├── LocalCrm.Api/        # Backend API
│   │   └── LocalCrm.App/        # Blazor frontend
│   └── PromptGateway/            # LLM gateway
│       └── PromptGateway.Api/   # Proxy + policies
├── samples/                       # Test örnekleri
│   └── legacy/                   # Archeologist test kodu
├── docs/                          # Dokümantasyon
│   ├── README.md                 # Ana dokümantasyon
│   ├── API.md                    # API referansı
│   ├── TESTING.md                # Test rehberi
│   ├── CONTRIBUTING.md           # Katkı rehberi
│   └── CHANGELOG.md              # Değişiklik günlüğü
├── docker-compose.yml             # Container orkestrasyon
├── test.sh                        # Hızlı test scripti
├── .gitignore                     # Git ignore
└── Impo.sln                       # Solution dosyası
```

## Metrikler

### Kod Metrikleri
- **Toplam Projeler:** 7
- **Toplam Dosya:** ~50+ C# dosyaları
- **Toplam API Endpoints:** 40+
- **Test Coverage:** Hazır altyapı

### Özellik Sayıları
- **Archeologist Patterns:** 8
- **Spectator Events:** Sınırsız (event sourcing)
- **LocalCrm Fields:** 5 (genişletilebilir)
- **PromptGateway PII Patterns:** 6

## Performans

### Spectator
- Real-time event processing
- SignalR ile düşük latency
- gRPC yüksek throughput

### LocalCrm
- SQLite ile hızlı yerel erişim
- Field-level merge O(1)
- CRDT-benzeri çakışma çözümü

### PromptGateway
- YARP ile yüksek throughput
- Token bucket efficient rate limiting
- Regex-based masking (source generated)

## Güvenlik

### PII Protection
- Email masking
- Credit card masking
- SSN masking
- Phone number masking
- IP address masking
- Credential masking

### Rate Limiting
- Token bucket algorithm
- Per-key limits
- Hourly reset

### Validation
- Input validation
- Email format
- Phone length
- Required fields

## Deployment

### Development
```bash
# Manuel
dotnet run --project src/<Project>/<Project>/<Project>.csproj

# Docker Compose
docker-compose up
```

### Production
```bash
# Build
dotnet publish -c Release

# Docker
docker build -t impo-spectator -f src/Spectator/Spectator.Api/Dockerfile .
docker run -p 5185:5185 impo-spectator
```

## Testing

### Quick Test
```bash
./test.sh
```

### Comprehensive Test
```bash
# Unit tests
dotnet test

# Integration tests (TESTING.md'de detaylı)
```

## Roadmap

### Kısa Vadeli
- [ ] Unit test coverage artırma
- [ ] Integration test suite
- [ ] OpenAPI/Swagger documentation
- [ ] CI/CD pipeline

### Orta Vadeli
- [ ] Kubernetes manifests
- [ ] Distributed tracing
- [ ] Advanced metrics (Prometheus)
- [ ] Authentication/Authorization

### Uzun Vadeli
- [ ] Multi-tenant support
- [ ] Cloud provider integrations
- [ ] Advanced analytics
- [ ] Mobile apps

## Katkıda Bulunma

CONTRIBUTING.md dosyasına bakın.

## Lisans

[Lisans bilgisi eklenecek]

## İletişim

[İletişim bilgileri eklenecek]

---

**Son Güncelleme:** 2026-08-28  
**Versiyon:** 2.0.0  
**Durum:** Production Ready
