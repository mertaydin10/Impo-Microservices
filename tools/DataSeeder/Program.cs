using System.Net.Http.Json;

Console.WriteLine("Impo örnek veri yükleyici\n");

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
    var device = "seeder";
    var now = DateTimeOffset.UtcNow;
    var contacts = new[]
    {
        Make(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), "Ayşe Güneş", "ayse.gunes@delta.example", "05321112233", "Delta Enerji", "Saha tableti siparişi", device, now),
        Make(Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"), "Burak Çelik", "burak.celik@orman.example", "05446667788", "Orman Mobilya", "ERP .NET geçişi", device, now.AddDays(-3)),
        Make(Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333"), "Deniz Koç", "deniz.koc@liman.example", "05359876543", "Kuzey Liman", "gRPC skor yayını", device, now.AddDays(-1))
    };

    var res = await client.PostAsJsonAsync($"{urls["LocalCrm"]}/sync", new { contacts });
    Console.WriteLine(res.IsSuccessStatusCode ? $"  CRM: {contacts.Length} kişi gönderildi" : $"  CRM hata: {await res.Content.ReadAsStringAsync()}");
}

if (urls.ContainsKey("Spectator"))
{
    const string matchId = "seeded-cup";
    var events = new (string Type, int Offset, string Json)[]
    {
        ("kickoff", 0, """{"note":"Kupa finali"}"""),
        ("goal", 600, """{"side":"home","player":"A. Yıldız"}"""),
        ("goal", 2100, """{"side":"away","player":"B. Kılıç"}"""),
        ("goal", 3300, """{"side":"home","player":"C. Acar"}""")
    };

    await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"{urls["Spectator"]}/api/matches/{matchId}"));
    foreach (var e in events)
    {
        await client.PostAsJsonAsync($"{urls["Spectator"]}/api/matches/{matchId}/events",
            new { type = e.Type, offsetSeconds = e.Offset, payloadJson = e.Json });
    }

    Console.WriteLine("  Spectator: seeded-cup maçı yazıldı");
}

if (urls.ContainsKey("PromptGateway"))
{
    await client.PostAsJsonAsync(
        $"{urls["PromptGateway"]}/v1/chat/completions",
        new { model = "gpt-4o-mini", messages = new[] { new { role = "user", content = "Sipariş durumu nedir?" } } });
    Console.WriteLine("  Gateway: örnek istek atıldı");
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
