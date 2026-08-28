using System.Net.Http.Json;
using System.Text.Json;

Console.WriteLine("🎬 Impo Demo Runner - Interactive workflow demonstration\n");

var client = new HttpClient();
var delay = 1500; // 1.5 seconds between steps

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

Console.WriteLine("═══════════════════════════════════════════════════════════");
Console.WriteLine("  DEMO: Cross-Service Integration Workflow");
Console.WriteLine("═══════════════════════════════════════════════════════════\n");

Console.WriteLine("📋 Scenario: Real-time match tracking with CRM integration\n");

// 1. Create a match
Console.WriteLine("🏟️  SPECTATOR: Creating live match...\n");
var matchId = "demo-real-vs-barca";

await Step("Creating kick-off event", async () =>
{
    await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "kick-off",
        offsetSeconds = 0,
        payloadJson = JsonSerializer.Serialize(new { stadium = "Santiago Bernabéu" })
    });
});

await Step("Scoring a goal at 23'", async () =>
{
    await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "goal",
        offsetSeconds = 23 * 60,
        payloadJson = JsonSerializer.Serialize(new { team = "Real Madrid", player = "Vinícius Jr", minute = 23 })
    });
});

await Step("Yellow card at 45'", async () =>
{
    await client.PostAsJsonAsync($"http://localhost:5185/api/matches/{matchId}/events", new
    {
        type = "yellow-card",
        offsetSeconds = 45 * 60,
        payloadJson = JsonSerializer.Serialize(new { team = "Barcelona", player = "Pedri", minute = 45 })
    });
});

// 2. Check match state
Console.WriteLine("\n📊 Checking match state...\n");

await Step("Fetching current match state", async () =>
{
    var response = await client.GetAsync($"http://localhost:5185/api/matches/{matchId}/replay?at=1500");
    var data = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"\n    State: {data.Substring(0, Math.Min(100, data.Length))}...");
});

// 3. CRM Integration
Console.WriteLine("\n👥 LOCALCRM: Managing contacts...\n");

await Step("Creating new contact for match organizer", async () =>
{
    var contact = new
    {
        id = Guid.NewGuid(),
        name = new { value = "Match Organizer", updatedAt = DateTimeOffset.UtcNow, deviceId = "demo" },
        email = new { value = "organizer@realmadrid.com", updatedAt = DateTimeOffset.UtcNow, deviceId = "demo" },
        phone = new { value = "555-MATCH-01", updatedAt = DateTimeOffset.UtcNow, deviceId = "demo" },
        company = new { value = "Real Madrid CF", updatedAt = DateTimeOffset.UtcNow, deviceId = "demo" }
    };
    
    await client.PostAsJsonAsync("http://localhost:5057/sync", new { contacts = new[] { contact } });
});

await Step("Fetching all contacts", async () =>
{
    var response = await client.GetAsync("http://localhost:5057/contacts");
    var contacts = await response.Content.ReadFromJsonAsync<List<object>>();
    Console.WriteLine($"\n    Total contacts: {contacts?.Count ?? 0}");
});

// 4. PromptGateway test
Console.WriteLine("\n🤖 PROMPTGATEWAY: Testing PII masking...\n");

await Step("Checking budget limits", async () =>
{
    var response = await client.GetAsync("http://localhost:5047/admin/budget");
    var data = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"\n    {data.Substring(0, Math.Min(80, data.Length))}...");
});

// 5. Metrics
Console.WriteLine("\n📈 Collecting system metrics...\n");

await Step("Spectator metrics", async () =>
{
    var response = await client.GetAsync("http://localhost:5185/api/metrics");
    var metrics = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"\n    {metrics.Substring(0, Math.Min(100, metrics.Length))}...");
});

await Step("LocalCrm stats", async () =>
{
    var response = await client.GetAsync("http://localhost:5057/stats");
    var stats = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"\n    {stats}");
});

// Final summary
Console.WriteLine("\n═══════════════════════════════════════════════════════════");
Console.WriteLine("  ✅ Demo Complete!");
Console.WriteLine("═══════════════════════════════════════════════════════════\n");

Console.WriteLine("🎯 What happened:");
Console.WriteLine("  1. Created a live match with 3 events (kick-off, goal, yellow card)");
Console.WriteLine("  2. Queried match state at specific time (replay feature)");
Console.WriteLine("  3. Added a new contact to CRM system");
Console.WriteLine("  4. Checked PromptGateway budget and limits");
Console.WriteLine("  5. Collected metrics from all services");

Console.WriteLine("\n📊 View results:");
Console.WriteLine($"  • Match UI:        http://localhost:5185?match={matchId}");
Console.WriteLine("  • CRM Dashboard:   http://localhost:5209");
Console.WriteLine("  • API Docs:        http://localhost:5185/swagger");
Console.WriteLine("  • Project Dashboard: open dashboard.html");

Console.WriteLine("\n💡 Try these next:");
Console.WriteLine("  • Replay match at different times");
Console.WriteLine("  • Sync CRM data from 'mobile device'");
Console.WriteLine("  • Test PII masking with sensitive data");
Console.WriteLine("  • Check audit logs in PromptGateway");
