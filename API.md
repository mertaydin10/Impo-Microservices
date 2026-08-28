# API Documentation

Bu dokümantasyon, Impo projesi içindeki tüm servislerin API endpoint'lerini açıklar.

## Spectator API (Port 5185)

Event sourcing ve real-time match tracking.

### Health & Metrics

#### Health Check
```http
GET /health
```
**Response:** `200 OK` - Healthy

#### Get Metrics
```http
GET /api/metrics
```
**Response:**
```json
{
  "uptimeSeconds": 3600.5,
  "totalEventsByType": {
    "goal": 15,
    "card": 8,
    "sub": 12
  },
  "totalEventsByMatch": {
    "match-1": 20,
    "match-2": 15
  },
  "activeMatches": 2,
  "totalEvents": 35
}
```

### Match Management

#### Get All Matches
```http
GET /api/matches
```
**Response:**
```json
["match-1", "match-2", "demo"]
```

#### Get Match Events
```http
GET /api/matches/{matchId}/events
```
**Response:**
```json
[
  {
    "matchId": "match-1",
    "type": "goal",
    "payloadJson": "{\"side\":\"home\"}",
    "offsetSeconds": 45,
    "timestamp": "2026-08-28T12:00:00Z"
  }
]
```

#### Post Match Event
```http
POST /api/matches/{matchId}/events
Content-Type: application/json

{
  "type": "goal",
  "offsetSeconds": 45,
  "payloadJson": "{\"side\":\"home\",\"player\":\"John Doe\"}"
}
```
**Response:**
```json
{
  "evt": { /* MatchEvent */ },
  "snap": {
    "matchId": "match-1",
    "home": 2,
    "away": 1,
    "clockSeconds": 45,
    "events": [ /* events */ ]
  }
}
```

#### Replay Match at Time
```http
GET /api/matches/{matchId}/replay?at=120
```
**Response:**
```json
{
  "matchId": "match-1",
  "home": 1,
  "away": 1,
  "clockSeconds": 120,
  "events": [ /* events up to 120s */ ]
}
```

#### Delete Match
```http
DELETE /api/matches/{matchId}
```
**Response:** `204 No Content`

### SignalR Hub

#### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hub/match")
  .build();
```

#### Join Match
```javascript
await connection.invoke("JoinMatch", "match-1");
```

#### Events
```javascript
// Receive new events
connection.on("event", (event) => {
  console.log("Event:", event);
});

// Receive snapshot updates
connection.on("snapshot", (snapshot) => {
  console.log("Snapshot:", snapshot);
});
```

### gRPC (Port 5186)

```protobuf
service MatchIngest {
  rpc Push(MatchEvent) returns (MatchEventResponse);
}
```

---

## LocalCrm API (Port 5057)

Local-first CRM with field-level merging.

### Health & Stats

#### Health Check
```http
GET /health
```
**Response:** `200 OK` - Healthy

#### Get Statistics
```http
GET /stats
```
**Response:**
```json
{
  "totalContacts": 150,
  "lastUpdated": "2026-08-28T12:00:00Z"
}
```

### Contact Management

#### Get All Contacts
```http
GET /contacts
```
**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": {
      "value": "John Doe",
      "updatedAt": "2026-08-28T12:00:00Z",
      "deviceId": "device-1"
    },
    "email": { /* FieldClock */ },
    "phone": { /* FieldClock */ },
    "company": { /* FieldClock */ },
    "notes": { /* FieldClock */ },
    "deletedAt": "0001-01-01T00:00:00Z"
  }
]
```

#### Get Single Contact
```http
GET /contacts/{id}
```
**Response:** `200 OK` - ContactRecord
**Response:** `404 Not Found` - Contact doesn't exist

#### Sync Contacts
```http
POST /sync
Content-Type: application/json

{
  "contacts": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": {
        "value": "Jane Doe",
        "updatedAt": "2026-08-28T12:00:00Z",
        "deviceId": "device-2"
      },
      "email": {
        "value": "jane@example.com",
        "updatedAt": "2026-08-28T12:00:00Z",
        "deviceId": "device-2"
      }
    }
  ]
}
```
**Response:**
```json
{
  "contacts": [ /* merged contacts */ ],
  "errors": [ /* validation errors if any */ ]
}
```

#### Delete Contact
```http
DELETE /contacts/{id}
```
**Response:** `204 No Content`
**Response:** `404 Not Found`

### Validation Rules

- **ID**: Cannot be empty
- **Name**: Required
- **Email**: Must be valid email format (if provided)
- **Phone**: Minimum 10 characters (if provided)

---

## PromptGateway API (Port 5047)

LLM gateway with PII masking and rate limiting.

### Health

#### Health Check
```http
GET /health
```
**Response:** `200 OK` - Healthy

### LLM Proxy

#### Chat Completions
```http
POST /v1/chat/completions
Content-Type: application/json
X-Api-Key: team-a

{
  "model": "gpt-4o",
  "messages": [
    {
      "role": "user",
      "content": "Send email to john@example.com"
    }
  ]
}
```
**Response:**
```json
{
  "id": "cmpl-mock",
  "model": "company-strong",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "[company-strong] PII maskelenmiş girdi özeti: Send email to [EMAIL]"
      }
    }
  ]
}
```

**PII Masking:**
- Email → `[EMAIL]`
- Credit Card → `[CARD]`
- SSN → `[SSN]`
- Phone → `[PHONE]`
- IP Address → `[IP]`
- Credentials → `password:[REDACTED]`

**Rate Limiting:**
- 8000 tokens per hour per API key
- Returns `429 Too Many Requests` when exceeded

### Admin Endpoints

#### View Audit Log
```http
GET /admin/audit
```
**Response:**
```json
[
  {
    "at": "2026-08-28T12:00:00Z",
    "key": "team-a",
    "model": "gpt-4o",
    "tokens": 25,
    "path": "/v1/chat/completions",
    "preview": "Send email to [EMAIL]..."
  }
]
```

#### View Budget Status
```http
GET /admin/budget
```
**Response:**
```json
{
  "team-a": 2500,
  "team-b": 5000,
  "anon": 100
}
```

#### Reset Budget
```http
POST /admin/budget/reset/{key}
```
**Response:**
```json
{
  "message": "Budget reset for team-a"
}
```

#### View Model Routes
```http
GET /admin/routes
```
**Response:**
```json
{
  "gpt-4o": "company-strong",
  "claude-3": "company-strong",
  "gpt-4o-mini": "company-fast",
  "cheap": "company-fast"
}
```

#### Add Model Route
```http
POST /admin/routes
Content-Type: application/json

{
  "alias": "gpt-5",
  "target": "company-ultra"
}
```
**Response:**
```json
{
  "message": "Route added",
  "alias": "gpt-5",
  "target": "company-ultra"
}
```

---

## Error Responses

Tüm API'ler standart HTTP status kodları kullanır:

- `200 OK` - İşlem başarılı
- `201 Created` - Kaynak oluşturuldu
- `204 No Content` - İşlem başarılı, içerik yok
- `400 Bad Request` - Geçersiz istek
- `404 Not Found` - Kaynak bulunamadı
- `429 Too Many Requests` - Rate limit aşıldı
- `500 Internal Server Error` - Sunucu hatası

Hata formatı:
```json
{
  "error": "error_code",
  "message": "Human readable error message",
  "errors": [ /* detailed errors if applicable */ ]
}
```

## Rate Limits

| Service | Endpoint | Limit |
|---------|----------|-------|
| Spectator | All | None |
| LocalCrm | All | None |
| PromptGateway | /v1/* | 8000 tokens/hour/key |

## Authentication

### Spectator
- No authentication required

### LocalCrm
- No authentication required (use CORS for security)

### PromptGateway
- API Key required via `X-Api-Key` header or `Authorization` header
- Default key for testing: `team-a`, `team-b`

## CORS

LocalCrm API has CORS enabled for all origins (development mode).
Production deployment should restrict origins.

## WebSockets

Spectator API uses SignalR over WebSocket for real-time updates.
Connection URL: `ws://localhost:5185/hub/match`

## Examples

Tüm servisleri test etmek için örnek curl komutları README.md dosyasında bulunabilir.
