namespace Spectator.Api;

public sealed record MatchEvent(
    string MatchId,
    string Type,
    string PayloadJson,
    int OffsetSeconds,
    DateTimeOffset RecordedAt);

public sealed record MatchSnapshot(
    string MatchId,
    int Home,
    int Away,
    int ClockSeconds,
    IReadOnlyList<MatchEvent> AppliedEvents);
