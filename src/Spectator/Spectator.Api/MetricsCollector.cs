using System.Collections.Concurrent;

namespace Spectator.Api;

public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _eventCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, long> _matchEventCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    public void RecordEvent(string matchId, string eventType)
    {
        _eventCounts.AddOrUpdate(eventType, 1, (_, count) => count + 1);
        _matchEventCounts.AddOrUpdate(matchId, 1, (_, count) => count + 1);
    }

    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot(
            UptimeSeconds: (DateTimeOffset.UtcNow - _startTime).TotalSeconds,
            TotalEventsByType: _eventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            TotalEventsByMatch: _matchEventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            ActiveMatches: _matchEventCounts.Count,
            TotalEvents: _eventCounts.Values.Sum()
        );
    }
}

public sealed record MetricsSnapshot(
    double UptimeSeconds,
    IReadOnlyDictionary<string, long> TotalEventsByType,
    IReadOnlyDictionary<string, long> TotalEventsByMatch,
    int ActiveMatches,
    long TotalEvents
);
