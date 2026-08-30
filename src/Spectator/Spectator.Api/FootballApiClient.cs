using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Spectator.Api;

public sealed class FootballApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public FootballApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(
            configuration["API_FOOTBALL_BASE_URL"] ?? "https://v3.football.api-sports.io");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _apiKey = configuration["API_FOOTBALL_KEY"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public Task<IReadOnlyList<FootballTeam>> GetTeamsAsync(
        string country,
        CancellationToken cancellationToken) =>
        GetCachedAsync(
            $"teams:{country}",
            TimeSpan.FromHours(12),
            async () =>
            {
                var result = await SendAsync<TeamEnvelope>(
                    $"/teams?country={Uri.EscapeDataString(country)}",
                    cancellationToken);
                return (IReadOnlyList<FootballTeam>)result.Response
                    .Select(item => item.Team)
                    .OrderBy(team => team.Name)
                    .ToList();
            });

    public Task<IReadOnlyList<FootballPlayer>> GetSquadAsync(
        int teamId,
        CancellationToken cancellationToken) =>
        GetCachedAsync(
            $"squad:{teamId}",
            TimeSpan.FromHours(6),
            async () =>
            {
                var result = await SendAsync<SquadEnvelope>(
                    $"/players/squads?team={teamId}",
                    cancellationToken);
                return (IReadOnlyList<FootballPlayer>)(result.Response.FirstOrDefault()?.Players ?? [])
                    .OrderBy(player => player.Position)
                    .ThenBy(player => player.Number ?? int.MaxValue)
                    .ThenBy(player => player.Name)
                    .ToList();
            });

    private async Task<T> SendAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new FootballApiUnavailableException(
                "API-Football anahtarı tanımlı değil. Yerel .env dosyasına API_FOOTBALL_KEY ekleyin.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-apisports-key", _apiKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new FootballApiUnavailableException(
                $"API-Football isteği başarısız ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new FootballApiUnavailableException("API-Football boş yanıt döndürdü.");
    }

    private async Task<IReadOnlyList<T>> GetCachedAsync<T>(
        string key,
        TimeSpan duration,
        Func<Task<IReadOnlyList<T>>> factory)
    {
        if (_cache.TryGetValue(key, out var cached) &&
            cached.ExpiresAt > DateTimeOffset.UtcNow &&
            cached.Value is IReadOnlyList<T> cachedItems)
        {
            return cachedItems;
        }

        var items = await factory();
        _cache[key] = new CacheEntry(DateTimeOffset.UtcNow.Add(duration), items);
        return items;
    }

    private sealed record CacheEntry(DateTimeOffset ExpiresAt, object Value);
    private sealed record TeamEnvelope(
        [property: JsonPropertyName("response")] List<TeamResult> Response);
    private sealed record TeamResult(
        [property: JsonPropertyName("team")] FootballTeam Team);
    private sealed record SquadEnvelope(
        [property: JsonPropertyName("response")] List<SquadResult> Response);
    private sealed record SquadResult(
        [property: JsonPropertyName("players")] List<FootballPlayer> Players);
}

public sealed record FootballTeam(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("founded")] int? Founded,
    [property: JsonPropertyName("logo")] string? Logo);

public sealed record FootballPlayer(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("age")] int? Age,
    [property: JsonPropertyName("number")] int? Number,
    [property: JsonPropertyName("position")] string? Position,
    [property: JsonPropertyName("photo")] string? Photo);

public sealed class FootballApiUnavailableException(string message) : Exception(message);
