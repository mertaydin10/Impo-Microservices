using System.Collections.Concurrent;
using System.Text.Json;

namespace Spectator.Api;

public sealed class EventStore
{
    private readonly ConcurrentDictionary<string, List<MatchEvent>> _streams = new(StringComparer.OrdinalIgnoreCase);

    public MatchEvent Append(string matchId, string type, string payloadJson, int offsetSeconds)
    {
        var evt = new MatchEvent(matchId, type, payloadJson, offsetSeconds, DateTimeOffset.UtcNow);
        var list = _streams.GetOrAdd(matchId, _ => []);
        lock (list)
        {
            list.Add(evt);
        }

        return evt;
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

        return new MatchSnapshot(matchId, home, away, atSeconds, events);
    }

    public IReadOnlyList<string> GetAllMatches() => _streams.Keys.ToList();

    public void Clear(string matchId) => _streams.TryRemove(matchId, out _);

    public int TotalEventsCount => _streams.Values.Sum(list =>
    {
        lock (list) return list.Count;
    });
}
