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
    const int matchCount = 20;
    var teams = new[]
    {
        "Adana", "Ankara", "Antalya", "Bursa", "Denizli", "Eskişehir", "Gaziantep", "İstanbul",
        "İzmir", "Kayseri", "Konya", "Mersin", "Samsun", "Sivas", "Trabzon", "Rize",
        "Sakarya", "Kocaeli", "Manisa", "Malatya", "Hatay", "Ordu", "Boluspor", "Göztepe"
    };
    var playerNames = new[]
    {
        "A. Yıldız", "B. Kılıç", "C. Acar", "D. Kaya", "E. Şahin", "F. Demir",
        "G. Arslan", "H. Koç", "K. Tekin", "M. Öztürk", "O. Güneş", "S. Yılmaz"
    };
    var random = new Random(42);
    var totalEvents = 0;

    for (var matchIndex = 1; matchIndex <= matchCount; matchIndex++)
    {
        var matchId = $"lig-{matchIndex:00}";
        var homeTeam = teams[(matchIndex - 1) % teams.Length];
        var awayTeam = teams[(matchIndex + 7) % teams.Length];
        await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{urls["Spectator"]}/api/matches/{matchId}"));
        await client.PostAsJsonAsync($"{urls["Spectator"]}/api/matches", new { matchId, homeTeam, awayTeam });
        await AddEvent(client, urls["Spectator"], matchId, "kickoff", 0, null, null, "Lig karşılaşması başladı");
        totalEvents++;

        var minute = 4;
        var eventCount = random.Next(14, 25);
        for (var eventIndex = 0; eventIndex < eventCount; eventIndex++)
        {
            minute = Math.Min(89, minute + random.Next(2, 7));
            var roll = random.Next(100);
            var type = roll < 32 ? "goal" : roll < 67 ? "card" : "sub";
            var side = random.Next(2) == 0 ? "home" : "away";
            var player = playerNames[random.Next(playerNames.Length)];
            var playerIn = type == "sub" ? playerNames[random.Next(playerNames.Length)] : null;
            var playerOut = type == "sub" ? player : null;
            await AddEvent(client, urls["Spectator"], matchId, type, minute, side,
                type == "sub" ? null : player, null, playerIn, playerOut);
            totalEvents++;
        }

        await AddEvent(client, urls["Spectator"], matchId, "fulltime", 90, null, null, "Karşılaşma sona erdi");
        totalEvents++;
    }

    Console.WriteLine($"  Spectator: {matchCount} maç ve {totalEvents} olay yazıldı");
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

static Task AddEvent(
    HttpClient client,
    string baseUrl,
    string matchId,
    string type,
    int minute,
    string? side,
    string? player,
    string? note = null,
    string? playerIn = null,
    string? playerOut = null) =>
    client.PostAsJsonAsync(
        $"{baseUrl}/api/matches/{matchId}/events",
        new { type, minute, side, player, note, playerIn, playerOut });
