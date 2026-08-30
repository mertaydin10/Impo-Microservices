using System.Collections.Concurrent;
using System.Text.Json;

namespace Spectator.Api;

public sealed class EventStore
{
    private readonly ConcurrentDictionary<string, List<MatchEvent>> _streams = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, MatchTeams> _teams = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _persistenceGate = new();
    private readonly string? _storagePath;

    public EventStore()
    {
    }

    public EventStore(string storagePath)
    {
        _storagePath = storagePath;
        Load();
    }

    public void SetTeams(
        string matchId,
        string home,
        string away,
        int? homeTeamId = null,
        int? awayTeamId = null,
        string? homeLogo = null,
        string? awayLogo = null)
    {
        _teams[matchId] = new MatchTeams(
            home,
            away,
            homeTeamId,
            awayTeamId,
            homeLogo,
            awayLogo);
        Persist();
    }

    public MatchTeams? GetTeams(string matchId) =>
        _teams.TryGetValue(matchId, out var teams) ? teams : null;

    public MatchEvent Append(string matchId, string type, string payloadJson, int offsetSeconds)
    {
        var evt = new MatchEvent(matchId, type, payloadJson, offsetSeconds, DateTimeOffset.UtcNow);
        var list = _streams.GetOrAdd(matchId, _ => []);
        lock (list)
        {
            if (list.Any(item => string.Equals(item.Type, "fulltime", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Biten maça yeni olay eklenemez.");
            }

            list.Add(evt);
        }

        Persist();
        return evt;
    }

    public bool Exists(string matchId) =>
        _streams.ContainsKey(matchId) || _teams.ContainsKey(matchId);

    public bool IsFinished(string matchId)
    {
        if (!_streams.TryGetValue(matchId, out var list))
        {
            return false;
        }

        lock (list)
        {
            return list.Any(item => string.Equals(item.Type, "fulltime", StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<MatchEvent> Stream(string matchId)
    {
        if (!_streams.TryGetValue(matchId, out var list))
        {
            return [];
        }

        lock (list)
        {
            return list.ToArray();
        }
    }

    public MatchSnapshot Replay(string matchId, int atSeconds)
    {
        var events = Stream(matchId).Where(e => e.OffsetSeconds <= atSeconds).OrderBy(e => e.OffsetSeconds).ToList();
        var home = 0;
        var away = 0;
        var homeName = "HOME";
        var awayName = "AWAY";
        if (_teams.TryGetValue(matchId, out var teams))
        {
            homeName = teams.Home;
            awayName = teams.Away;
        }

        foreach (var e in events)
        {
            if (!string.Equals(e.Type, "goal", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(e.PayloadJson) ? "{}" : e.PayloadJson);
            var side = doc.RootElement.TryGetProperty("side", out var s) ? s.GetString() : null;
            if (string.Equals(side, "home", StringComparison.OrdinalIgnoreCase))
            {
                home++;
            }
            else if (string.Equals(side, "away", StringComparison.OrdinalIgnoreCase))
            {
                away++;
            }
        }

        return new MatchSnapshot(matchId, homeName, awayName, home, away, atSeconds, events);
    }

    public IReadOnlyList<string> GetAllMatches() => _streams.Keys.Concat(_teams.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    public void Clear(string matchId)
    {
        _streams.TryRemove(matchId, out _);
        _teams.TryRemove(matchId, out _);
        Persist();
    }

    public int TotalEventsCount => _streams.Values.Sum(list =>
    {
        lock (list) return list.Count;
    });

    private void Load()
    {
        if (_storagePath is null || !File.Exists(_storagePath))
        {
            return;
        }

        try
        {
            var matches = JsonSerializer.Deserialize<List<StoredMatch>>(File.ReadAllText(_storagePath)) ?? [];
            foreach (var match in matches)
            {
                _teams[match.Id] = new MatchTeams(
                    match.HomeTeam,
                    match.AwayTeam,
                    match.HomeTeamId,
                    match.AwayTeamId,
                    match.HomeLogo,
                    match.AwayLogo);
                _streams[match.Id] = match.Events;
            }
        }
        catch (JsonException)
        {
            // Bozuk dosya uygulamanın açılmasını engellemez; yeni kayıtla yeniden oluşturulur.
        }
    }

    private void Persist()
    {
        if (_storagePath is null)
        {
            return;
        }

        lock (_persistenceGate)
        {
            var ids = GetAllMatches();
            var matches = ids.Select(id =>
            {
                var teams = _teams.TryGetValue(id, out var names)
                    ? names
                    : new MatchTeams("HOME", "AWAY");
                var events = Stream(id).ToList();
                return new StoredMatch(
                    id,
                    teams.Home,
                    teams.Away,
                    teams.HomeTeamId,
                    teams.AwayTeamId,
                    teams.HomeLogo,
                    teams.AwayLogo,
                    events);
            }).ToList();

            var directory = Path.GetDirectoryName(_storagePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = $"{_storagePath}.tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(matches));
            File.Move(temporaryPath, _storagePath, true);
        }
    }

    private sealed record StoredMatch(
        string Id,
        string HomeTeam,
        string AwayTeam,
        int? HomeTeamId,
        int? AwayTeamId,
        string? HomeLogo,
        string? AwayLogo,
        List<MatchEvent> Events);
}
