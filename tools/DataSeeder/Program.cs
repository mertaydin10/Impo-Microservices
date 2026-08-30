using System.Net.Http.Json;

Console.WriteLine("Impo toplu veri yükleyici\n");

var candidates = new Dictionary<string, string>
{
    ["LocalCrm"] = "http://localhost:5057",
    ["Spectator"] = "http://localhost:5185",
    ["PromptGateway"] = "http://localhost:5047"
};

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
var urls = new Dictionary<string, string>();

foreach (var (name, url) in candidates)
{
    try
    {
        if ((await client.GetAsync($"{url}/health")).IsSuccessStatusCode)
        {
            urls[name] = url;
            Console.WriteLine($"  {name} açık");
        }
        else
        {
            Console.WriteLine($"  {name} kapalı");
        }
    }
    catch
    {
        Console.WriteLine($"  {name} kapalı — atlanıyor");
    }
}

if (urls.ContainsKey("LocalCrm"))
{
    const int contactCount = 250;
    const string device = "bulk-seeder";
    var now = DateTimeOffset.UtcNow;
    var firstNames = new[]
    {
        "Ayşe", "Burak", "Ceren", "Deniz", "Ece", "Emre", "Gökçe", "Hakan", "İrem", "Kerem",
        "Leyla", "Mert", "Nehir", "Okan", "Pelin", "Selin", "Tolga", "Yasemin", "Can", "Zeynep"
    };
    var lastNames = new[]
    {
        "Acar", "Arslan", "Aydın", "Çelik", "Demir", "Doğan", "Güneş", "Kaya", "Kılıç", "Koç",
        "Öztürk", "Şahin", "Tekin", "Yıldırım", "Yılmaz"
    };
    var companies = new[]
    {
        "Atlas Teknoloji", "Delta Enerji", "Kuzey Lojistik", "Mavi Yazılım", "Nova Sağlık", "Orman Mobilya",
        "Pusula Danışmanlık", "Rota Otomotiv", "Sahil Gıda", "Vadi İnşaat", "Yakamoz Medya", "Zirve Finans"
    };
    var topics = new[]
    {
        "Yeni müşteri görüşmesi", "Teklif bekliyor", "Demo planlanacak", "Yıllık sözleşme yenileme",
        "Teknik ekip dönüş yapacak", "Fiyatlandırma görüşmesi", "Pilot uygulama başladı", "Satış sonrası destek"
    };
    var contacts = Enumerable.Range(1, contactCount).Select(index =>
    {
        var firstName = firstNames[(index - 1) % firstNames.Length];
        var lastName = lastNames[((index - 1) / firstNames.Length) % lastNames.Length];
        var emailName = $"{Normalize(firstName)}.{Normalize(lastName)}{index}";
        var timestamp = now.AddMinutes(-index * 37);
        return Make(
            Guid.Parse($"10000000-0000-0000-0000-{index:000000000000}"),
            $"{firstName} {lastName}",
            $"{emailName}@example.com",
            $"05{30 + index % 20:00}{1000000 + index:0000000}",
            companies[index % companies.Length],
            topics[index % topics.Length],
            device,
            timestamp);
    }).ToArray();

    var successfulContacts = 0;
    foreach (var batch in contacts.Chunk(50))
    {
        var response = await client.PostAsJsonAsync($"{urls["LocalCrm"]}/sync", new { contacts = batch });
        if (response.IsSuccessStatusCode)
        {
            successfulContacts += batch.Length;
        }
        else
        {
            Console.WriteLine($"  CRM hata: {await response.Content.ReadAsStringAsync()}");
        }
    }
    Console.WriteLine($"  CRM: {successfulContacts} kişi gönderildi");
}

if (urls.ContainsKey("Spectator"))
{
    Console.WriteLine("  Spectator: sentetik veri atlandı; kulüp ve oyuncular API-Football üzerinden seçilir");
}

if (urls.ContainsKey("PromptGateway"))
{
    for (var index = 1; index <= 20; index++)
    {
        await client.PostAsJsonAsync(
            $"{urls["PromptGateway"]}/v1/chat/completions",
            new
            {
                model = index % 3 == 0 ? "quality" : "fast",
                messages = new[] { new { role = "user", content = $"{index:000} numaralı siparişin durumu nedir?" } }
            });
    }
    Console.WriteLine("  Gateway: 20 örnek istek atıldı");
}

Console.WriteLine("\nBitti. CRM http://localhost:5209  |  Maç http://localhost:5185");

static object Make(Guid id, string name, string email, string phone, string company, string notes, string device, DateTimeOffset at) => new
{
    id,
    name = F(name, at, device),
    email = F(email, at, device),
    phone = F(phone, at, device),
    company = F(company, at, device),
    notes = F(notes, at, device)
};

static object F(string value, DateTimeOffset at, string device) => new { value, updatedAt = at, deviceId = device };

static string Normalize(string value) => value
    .ToLowerInvariant()
    .Replace("ç", "c")
    .Replace("ğ", "g")
    .Replace("ı", "i")
    .Replace("ö", "o")
    .Replace("ş", "s")
    .Replace("ü", "u");
