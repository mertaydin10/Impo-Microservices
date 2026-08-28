# Katkıda Bulunma Rehberi

Impo projesine katkıda bulunmak istediğiniz için teşekkürler! Bu rehber, projeye nasıl katkıda bulunabileceğinizi açıklar.

## Geliştirme Ortamı

### Gereksinimler
- .NET 10 SDK
- Visual Studio 2022 veya VS Code
- Git

### Projeyi Klonlama
```bash
git clone <repo-url>
cd impo
dotnet restore
dotnet build
```

### Testleri Çalıştırma
```bash
dotnet test
```

## Kod Standartları

### C# Kod Stili
- Modern C# özellikleri kullanın (records, pattern matching, vb.)
- Nullable reference types etkin (`<Nullable>enable</Nullable>`)
- Minimal API pattern tercih edilir
- File-scoped namespaces kullanın

### İsimlendirme Kuralları
```csharp
// Classes, Records, Enums - PascalCase
public sealed class EventStore { }
public record ContactRecord(Guid Id, string Name);

// Methods - PascalCase
public void ProcessEvent() { }

// Parameters, Local Variables - camelCase
var userName = "test";
void SaveUser(string userId) { }

// Private fields - _camelCase
private readonly EventStore _store;

// Constants - PascalCase
private const int MaxRetries = 3;
```

### Async/Await Best Practices
```csharp
// ✅ Doğru
public async Task<Result> ProcessAsync(CancellationToken ct)
{
    var data = await FetchDataAsync(ct);
    return Process(data);
}

// ❌ Yanlış
public async Task<Result> ProcessAsync()
{
    var data = await FetchDataAsync().Result; // .Result kullanma!
    return Process(data);
}
```

## Pull Request Süreci

1. **Fork** edin ve yeni bir branch oluşturun
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Değişikliklerinizi yapın** ve commit edin
   ```bash
   git add .
   git commit -m "feat: add new feature"
   ```

3. **Test edin** - tüm testlerin geçtiğinden emin olun
   ```bash
   dotnet test
   ```

4. **Push edin** ve Pull Request açın
   ```bash
   git push origin feature/my-new-feature
   ```

## Commit Mesajları

Conventional Commits formatını kullanıyoruz:

```
feat: yeni özellik ekleme
fix: bug düzeltme
docs: dokümantasyon değişikliği
style: kod formatı (işlevselliği etkilemeyen)
refactor: kod yeniden yapılandırma
perf: performans iyileştirmesi
test: test ekleme/düzeltme
chore: build/config değişiklikleri
```

Örnekler:
```
feat(archeologist): add SQL injection detection
fix(spectator): resolve SignalR reconnection issue
docs(readme): update API documentation
```

## Proje Yapısı

```
src/
├── Archeologist/          # Kod analiz aracı
│   ├── Core/             # Roslyn-based analiz
│   └── Cli/              # CLI uygulaması
├── Spectator/            # Event sourcing API
│   └── Api/              # SignalR + gRPC
├── LocalCrm/             # Local-first CRM
│   ├── Shared/           # Shared models
│   ├── Api/              # Backend API
│   └── App/              # Blazor frontend
└── PromptGateway/        # LLM gateway
    └── Api/              # YARP proxy
```

## Yeni Özellik Ekleme

### 1. Archeologist'e Yeni Pattern Eklemek

```csharp
// CodeWalker.cs
public override void VisitYourNode(YourNodeSyntax node)
{
    if (IsProblematic(node))
    {
        Add(FindingKind.YourNewPattern, node,
            "Problem açıklaması",
            "Önerilen çözüm");
    }
    
    base.VisitYourNode(node);
}
```

### 2. Spectator'a Yeni Event Tipi Eklemek

```csharp
// EventStore.cs - Replay metodunda
if (string.Equals(e.Type, "yourtype", StringComparison.OrdinalIgnoreCase))
{
    // Event işleme mantığı
}
```

### 3. LocalCrm'e Yeni Alan Eklemek

```csharp
// ContactRecord.cs
public sealed class ContactRecord
{
    // Mevcut alanlar...
    public FieldClock YourNewField { get; set; } = new();
}

// FieldMerger.cs - Merge metodunda
public static ContactRecord Merge(ContactRecord local, ContactRecord remote) => new()
{
    // Mevcut alanlar...
    YourNewField = Pick(local.YourNewField, remote.YourNewField)
};
```

### 4. PromptGateway'e Yeni PII Pattern Eklemek

```csharp
// Policy.cs - PiiMasker
[GeneratedRegex(@"your-regex-pattern")]
private static partial Regex YourPattern();

public static string Mask(string input)
{
    var t = YourPattern().Replace(input, "[MASKED]");
    // Diğer maskeler...
    return t;
}
```

## Test Yazma

Her yeni özellik için test yazın:

```csharp
[Fact]
public async Task Should_ProcessEvent_Successfully()
{
    // Arrange
    var store = new EventStore();
    
    // Act
    var result = store.Append("match-1", "goal", "{}", 10);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("goal", result.Type);
}
```

## Dokümantasyon

- Public API'ler için XML dokümantasyon ekleyin
- README'yi güncelleyin
- CHANGELOG'a ekleyin

```csharp
/// <summary>
/// Processes the incoming event and updates the store.
/// </summary>
/// <param name="matchId">The unique match identifier</param>
/// <param name="type">Event type (goal, card, sub)</param>
/// <returns>The created event</returns>
public MatchEvent Append(string matchId, string type, ...)
```

## Sorular?

- Issue açın
- Mevcut issue'ları inceleyin
- Discussions bölümünü kullanın

## Lisans

Katkıda bulunarak, katkılarınızın projenin lisansı altında olduğunu kabul etmiş olursunuz.
