using System.Net.Http.Json;

Console.WriteLine("Impo Demo Runner\n");

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
var delay = 800;

async Task Step(string description, Func<Task> action)
{
    Console.Write($"  ▸ {description}... ");
    try
    {
        await action();
        Console.WriteLine("✓");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ ({ex.Message})");
    }
}

static async Task EnsureHealthy(HttpClient client, string name, string url)
{
    try
    {
        var ok = (await client.GetAsync($"{url}/health")).IsSuccessStatusCode;
        Console.WriteLine(ok ? $"  {name} açık" : $"  {name} kapalı");
    }
    catch
    {
        Console.WriteLine($"  {name} kapalı");
    }
}

await EnsureHealthy(client, "Spectator", "http://localhost:5185");
await EnsureHealthy(client, "LocalCrm", "http://localhost:5057");
await EnsureHealthy(client, "PromptGateway", "http://localhost:5047");
Console.WriteLine();

var matchId = $"demo-{DateTimeOffset.UtcNow:HHmmss}";

Console.WriteLine("SPECTATOR\n");
await Step("Demo maçı oluştur", async () =>
{
    var response = await client.PostAsJsonAsync("http://localhost:5185/api/matches", new
    {
        matchId,
        homeTeam = "Demo Ev",
        awayTeam = "Demo Dep",
        homeTeamId = 1001,
        awayTeamId = 1002,
        homeLogo = (string?)null,
        awayLogo = (string?)null
    });
    response.EnsureSuccessStatusCode();
});

await Step("Kickoff", async () =>
{
    (await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "kickoff",
        minute = 0,
        note = "Demo maçı başladı"
    })).EnsureSuccessStatusCode();
});

await Step("Gol 23'", async () =>
{
    (await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "goal",
        minute = 23,
        side = "home",
        player = "A. Yıldız"
    })).EnsureSuccessStatusCode();
});

await Step("Sarı kart 41'", async () =>
{
    (await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "card",
        minute = 41,
        side = "away",
        player = "B. Kaya",
        cardKind = "yellow"
    })).EnsureSuccessStatusCode();
});

await Step("Özet oku", async () =>
{
    var summary = await client.GetStringAsync($"http://localhost:5185/api/matches/{matchId}/summary");
    Console.WriteLine($"\n{summary.Split('\n').First()}");
});

Console.WriteLine("\nLOCALCRM\n");
await Step("Organizatör kişisini senkronla", async () =>
{
    var now = DateTimeOffset.UtcNow;
    var contact = new
    {
        id = Guid.NewGuid(),
        name = new { value = "Demo Organizatör", updatedAt = now, deviceId = "demo" },
        email = new { value = "organizator@example.com", updatedAt = now, deviceId = "demo" },
        phone = new { value = "05321234567", updatedAt = now, deviceId = "demo" },
        company = new { value = "Impo Demo", updatedAt = now, deviceId = "demo" },
        notes = new { value = $"Maç: {matchId}", updatedAt = now, deviceId = "demo" }
    };
    (await client.PostAsJsonAsync("http://localhost:5057/sync", new { contacts = new[] { contact } }))
        .EnsureSuccessStatusCode();
});

await Step("Kişi sayısını oku", async () =>
{
    var contacts = await client.GetFromJsonAsync<List<object>>("http://localhost:5057/contacts");
    Console.WriteLine($"\n    {contacts?.Count ?? 0} kişi");
});

Console.WriteLine("\nPROMPTGATEWAY\n");
await Step("PII maskeli istek gönder", async () =>
{
    (await client.PostAsJsonAsync("http://localhost:5047/v1/chat/completions", new
    {
        model = "gpt-4o",
        messages = new[]
        {
            new { role = "user", content = "Müşteri ayse@firma.com ve 05321112233 ile aradı." }
        }
    }, default)).EnsureSuccessStatusCode();
});

await Step("Audit kaydını oku", async () =>
{
    var audit = await client.GetFromJsonAsync<List<object>>("http://localhost:5047/admin/audit");
    Console.WriteLine($"\n    {audit?.Count ?? 0} kayıt");
});

Console.WriteLine("\nBitti.");
Console.WriteLine($"  Maç:  http://localhost:5185/?match={matchId}");
Console.WriteLine("  CRM:  http://localhost:5209");
Console.WriteLine("  Kapı: http://localhost:5047");
