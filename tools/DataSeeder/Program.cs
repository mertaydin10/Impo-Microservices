using System.Net.Http.Json;
using System.Text.Json;
using Bogus;

Console.WriteLine("🌱 Impo Data Seeder - Generating realistic test data...\n");

// Check if services are running
var baseUrls = new Dictionary<string, string>
{
    ["LocalCrm"] = "http://localhost:5057",
    ["Spectator"] = "http://localhost:5185",
    ["PromptGateway"] = "http://localhost:5047"
};

var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

// Check service availability
Console.WriteLine("📡 Checking service availability...");
var availableServices = new List<string>();
foreach (var (name, url) in baseUrls)
{
    try
    {
        var response = await client.GetAsync($"{url}/health");
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"  ✓ {name} is online");
            availableServices.Add(name);
        }
        else
        {
            Console.WriteLine($"  ✗ {name} is offline (will skip)");
        }
    }
    catch
    {
        Console.WriteLine($"  ✗ {name} is offline (will skip)");
    }
}
Console.WriteLine();

// Seed LocalCrm contacts
if (availableServices.Contains("LocalCrm"))
{
    Console.WriteLine("👥 Seeding LocalCrm contacts...");
    
    var contactFaker = new Faker<Contact>()
        .RuleFor(c => c.Id, f => Guid.NewGuid())
        .RuleFor(c => c.Name, f => new FieldClock 
        { 
            Value = f.Name.FullName(), 
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-f.Random.Int(0, 30)), 
            DeviceId = "seeder" 
        })
        .RuleFor(c => c.Email, f => new FieldClock 
        { 
            Value = f.Internet.Email(), 
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-f.Random.Int(0, 30)), 
            DeviceId = "seeder" 
        })
        .RuleFor(c => c.Phone, f => new FieldClock 
        { 
            Value = f.Phone.PhoneNumber("###-###-####"), 
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-f.Random.Int(0, 30)), 
            DeviceId = "seeder" 
        })
        .RuleFor(c => c.Company, f => new FieldClock 
        { 
            Value = f.Company.CompanyName(), 
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-f.Random.Int(0, 30)), 
            DeviceId = "seeder" 
        });

    var contacts = contactFaker.Generate(20);
    
    try
    {
        var syncBatch = new { contacts };
        var response = await client.PostAsJsonAsync($"{baseUrls["LocalCrm"]}/sync", syncBatch);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"  ✓ Created {contacts.Count} contacts");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"  ✗ Failed: {error}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✗ Error: {ex.Message}");
    }
}

// Seed Spectator match events
if (availableServices.Contains("Spectator"))
{
    Console.WriteLine("\n🏟️  Seeding Spectator match events...");
    
    var matchIds = new[] { "match-real-vs-barca", "match-chelsea-vs-bayern", "match-psg-vs-milan" };
    var eventTypes = new[] { "kick-off", "goal", "yellow-card", "substitution", "half-time", "full-time" };
    
    foreach (var matchId in matchIds)
    {
        var eventCount = Random.Shared.Next(8, 15);
        
        for (int i = 0; i < eventCount; i++)
        {
            var eventDto = new
            {
                type = eventTypes[Random.Shared.Next(eventTypes.Length)],
                offsetSeconds = Random.Shared.Next(1, 90) * 60,
                payloadJson = JsonSerializer.Serialize(new { player = $"Player {i + 1}", minute = Random.Shared.Next(1, 90) })
            };
            
            try
            {
                await client.PostAsJsonAsync($"{baseUrls["Spectator"]}/api/matches/{matchId}/events", eventDto);
            }
            catch { }
        }
        
        Console.WriteLine($"  ✓ Created {eventCount} events for {matchId}");
    }
}

Console.WriteLine("\n✅ Data seeding complete!");
Console.WriteLine("\n📊 You can now:");
Console.WriteLine("  • View contacts: http://localhost:5209");
Console.WriteLine("  • View matches: http://localhost:5185");
Console.WriteLine("  • Check API: http://localhost:5057/swagger");

// Models
public record Contact(
    Guid Id,
    FieldClock Name,
    FieldClock Email,
    FieldClock Phone,
    FieldClock Company
);

public record FieldClock
{
    public required string Value { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string DeviceId { get; init; }
}
