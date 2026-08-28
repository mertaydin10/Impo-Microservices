# Test Rehberi

Bu dokümantasyon, Impo projesini test etmek için kullanabileceğiniz komutları ve senaryoları içerir.

## Hızlı Başlangıç

### Tüm Servisleri Başlatma

#### Seçenek 1: Docker Compose (Önerilen)
```bash
docker-compose up -d
```

#### Seçenek 2: Manuel
Terminal'de her servisi ayrı ayrı başlatın:

```bash
# Terminal 1 - Spectator
dotnet run --project src/Spectator/Spectator.Api --launch-profile http

# Terminal 2 - LocalCrm API
dotnet run --project src/LocalCrm/LocalCrm.Api --launch-profile http

# Terminal 3 - LocalCrm App
dotnet run --project src/LocalCrm/LocalCrm.App --launch-profile http

# Terminal 4 - PromptGateway
dotnet run --project src/PromptGateway/PromptGateway.Api --launch-profile http
```

### Health Check

Tüm servislerin çalıştığını kontrol edin:

```bash
# Spectator
curl http://localhost:5185/health

# LocalCrm API
curl http://localhost:5057/health

# PromptGateway
curl http://localhost:5047/health
```

## 1. Archeologist Test Senaryoları

### Temel Tarama
```bash
# Basit tarama
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy

# Markdown rapor oluştur
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy -o report.md

# JSON formatında
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy --json -o report.json

# Sadece kritik bulguları göster
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy --min-severity CRITICAL

# Sadece yüksek ve kritik bulguları göster
dotnet run --project src/Archeologist/Archeologist.Cli -- samples/legacy --min-severity HIGH
```

### Test Kodu Oluşturma

Archeologist'i test etmek için örnek problemli kod:

```csharp
// samples/legacy/TestProblems.cs
public class TestProblems
{
    // HttpClient leak
    public void BadHttpClient()
    {
        var client = new HttpClient();
        client.GetAsync("https://api.example.com").Wait();
    }

    // Sync over async
    public string BadAsync()
    {
        return GetDataAsync().Result;
    }

    // DateTime.Now
    public void BadDateTime()
    {
        var now = DateTime.Now;
        Log(now.ToString());
    }

    // Secret in code
    public void BadSecret()
    {
        var apiKey = "sk-1234567890abcdef";
        var password = "MyP@ssw0rd123";
    }

    // SQL Injection
    public void BadSql(string userId)
    {
        var query = "SELECT * FROM Users WHERE Id = " + userId;
        ExecuteQuery(query);
    }

    // Empty catch
    public void BadErrorHandling()
    {
        try
        {
            RiskyOperation();
        }
        catch
        {
        }
    }

    // String concat in loop
    public string BadLoop(List<string> items)
    {
        string result = "";
        foreach (var item in items)
        {
            result += item + ",";
        }
        return result;
    }
}
```

## 2. Spectator Test Senaryoları

### Web UI
http://localhost:5185 adresini tarayıcıda açın ve:
1. "Olay gönder" butonuna tıklayın
2. Skorun değiştiğini gözlemleyin
3. Replay kaydırıcısını kullanın
4. Maç ID'sini değiştirin ve yeni maç oluşturun

### API Testleri

```bash
# Yeni maç eventi gönder
curl -X POST http://localhost:5185/api/matches/test-match/events \
  -H "Content-Type: application/json" \
  -d '{
    "type": "goal",
    "offsetSeconds": 45,
    "payloadJson": "{\"side\":\"home\",\"player\":\"Messi\"}"
  }'

# Maç eventlerini listele
curl http://localhost:5185/api/matches/test-match/events

# 60. saniyede replay
curl http://localhost:5185/api/matches/test-match/replay?at=60

# Tüm maçları listele
curl http://localhost:5185/api/matches

# Metrics görüntüle
curl http://localhost:5185/api/metrics

# Maçı temizle
curl -X DELETE http://localhost:5185/api/matches/test-match
```

### SignalR Test (JavaScript)

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5185/hub/match")
  .withAutomaticReconnect()
  .build();

connection.on("event", (event) => {
  console.log("New event:", event);
});

connection.on("snapshot", (snapshot) => {
  console.log("Score update:", snapshot.home, "-", snapshot.away);
});

await connection.start();
await connection.invoke("JoinMatch", "test-match");
```

### Load Test

```bash
# 100 event gönder
for i in {1..100}; do
  curl -X POST http://localhost:5185/api/matches/load-test/events \
    -H "Content-Type: application/json" \
    -d "{\"type\":\"goal\",\"offsetSeconds\":$i,\"payloadJson\":\"{\\\"side\\\":\\\"home\\\"}\"}" &
done
wait
```

## 3. LocalCrm Test Senaryoları

### Web UI
- API: http://localhost:5057
- App: http://localhost:5209

### API Testleri

```bash
# Test verileri hazırla
TEST_ID="3fa85f64-5717-4562-b3fc-2c963f66afa6"
TEST_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Yeni contact ekle
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{
    "contacts": [
      {
        "id": "'$TEST_ID'",
        "name": {
          "value": "Test User",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        },
        "email": {
          "value": "test@example.com",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        },
        "phone": {
          "value": "+905551234567",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        },
        "company": {
          "value": "Test Company",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        }
      }
    ]
  }'

# Tüm contactları listele
curl http://localhost:5057/contacts

# Tek contact getir
curl http://localhost:5057/contacts/$TEST_ID

# İstatistikler
curl http://localhost:5057/stats

# Contact sil
curl -X DELETE http://localhost:5057/contacts/$TEST_ID

# Validasyon hatası testi (email geçersiz)
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{
    "contacts": [
      {
        "id": "'$(uuidgen)'",
        "name": {
          "value": "Invalid User",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        },
        "email": {
          "value": "not-an-email",
          "updatedAt": "'$TEST_DATE'",
          "deviceId": "device-1"
        }
      }
    ]
  }'
```

### Field-Level Merge Test

```bash
# Device 1: Name güncelle
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{
    "contacts": [
      {
        "id": "'$TEST_ID'",
        "name": {
          "value": "Updated Name",
          "updatedAt": "'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'",
          "deviceId": "device-1"
        }
      }
    ]
  }'

# Device 2: Email güncelle (farklı cihaz, aynı anda)
curl -X POST http://localhost:5057/sync \
  -H "Content-Type: application/json" \
  -d '{
    "contacts": [
      {
        "id": "'$TEST_ID'",
        "email": {
          "value": "newemail@example.com",
          "updatedAt": "'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'",
          "deviceId": "device-2"
        }
      }
    ]
  }'

# Her iki değişiklik de korunmuş olmalı
curl http://localhost:5057/contacts/$TEST_ID
```

## 4. PromptGateway Test Senaryoları

### Temel PII Masking

```bash
# Email masking
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "gpt-4o",
    "messages": [
      {
        "role": "user",
        "content": "Send email to john@example.com and jane@test.com"
      }
    ]
  }'

# Credit card masking
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "gpt-4o",
    "messages": [
      {
        "role": "user",
        "content": "My card is 4532-1234-5678-9010"
      }
    ]
  }'

# Phone masking
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "gpt-4o",
    "messages": [
      {
        "role": "user",
        "content": "Call me at 05551234567"
      }
    ]
  }'

# Multiple PII types
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "gpt-4o",
    "messages": [
      {
        "role": "user",
        "content": "Contact john@test.com at 05551234567, IP: 192.168.1.1, password: secret123"
      }
    ]
  }'
```

### Rate Limiting Test

```bash
# Hızlı istek gönder (rate limit test)
for i in {1..100}; do
  curl -X POST http://localhost:5047/v1/chat/completions \
    -H "Content-Type: application/json" \
    -H "X-Api-Key: rate-test" \
    -d '{
      "model": "gpt-4o",
      "messages": [{"role": "user", "content": "Test '$i'"}]
    }' &
done
wait

# Budget durumunu kontrol et
curl http://localhost:5047/admin/budget
```

### Model Routing Test

```bash
# Default model
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "gpt-4o",
    "messages": [{"role": "user", "content": "test"}]
  }'

# Custom model
curl -X POST http://localhost:5047/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: team-a" \
  -d '{
    "model": "cheap",
    "messages": [{"role": "user", "content": "test"}]
  }'

# Yeni route ekle
curl -X POST http://localhost:5047/admin/routes \
  -H "Content-Type: application/json" \
  -d '{
    "alias": "custom-model",
    "target": "company-custom"
  }'

# Tüm route'ları görüntüle
curl http://localhost:5047/admin/routes
```

### Admin Tests

```bash
# Audit log
curl http://localhost:5047/admin/audit | jq '.[0:5]'

# Budget snapshot
curl http://localhost:5047/admin/budget | jq

# Budget reset
curl -X POST http://localhost:5047/admin/budget/reset/team-a
```

## Performance Testing

### Apache Bench

```bash
# Spectator
ab -n 1000 -c 10 -p event.json -T application/json \
  http://localhost:5185/api/matches/perf-test/events

# LocalCrm
ab -n 1000 -c 10 http://localhost:5057/contacts

# PromptGateway
ab -n 1000 -c 10 -p prompt.json -T application/json \
  -H "X-Api-Key: perf-test" \
  http://localhost:5047/v1/chat/completions
```

### wrk

```bash
# Spectator load test
wrk -t4 -c100 -d30s http://localhost:5185/api/matches

# LocalCrm load test
wrk -t4 -c100 -d30s http://localhost:5057/contacts
```

## Automated Testing

```bash
# Tüm testleri çalıştır
dotnet test

# Specific project
dotnet test src/Archeologist/Archeologist.Core.Tests

# Coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Debugging

### Logs
Tüm servisler console'a log yazıyor. Detaylı log için:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
```

### Docker Logs

```bash
# Tüm servislerin logları
docker-compose logs -f

# Belirli servis
docker-compose logs -f spectator
```

## Temizleme

```bash
# Docker containers
docker-compose down -v

# Build artifacts
dotnet clean

# Database files
rm src/LocalCrm/LocalCrm.Api/cloud.db
```

## Troubleshooting

### Port already in use
```bash
# macOS/Linux
lsof -ti:5185 | xargs kill -9

# Windows
netstat -ano | findstr :5185
taskkill /PID <PID> /F
```

### Database locked
```bash
rm src/LocalCrm/LocalCrm.Api/cloud.db*
```

### Docker issues
```bash
docker-compose down
docker system prune -a
docker-compose up --build
```
