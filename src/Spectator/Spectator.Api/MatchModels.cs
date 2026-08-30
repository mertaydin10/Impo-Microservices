using System.Text.Json;

namespace Spectator.Api;

public sealed record MatchEvent(
    string MatchId,
    string Type,
    string PayloadJson,
    int OffsetSeconds,
    DateTimeOffset RecordedAt);

public sealed record MatchSnapshot(
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    int Home,
    int Away,
    int ClockSeconds,
    IReadOnlyList<MatchEvent> AppliedEvents);

public sealed record MatchEventView(
    string MatchId,
    string Type,
    string TypeLabel,
    string? Side,
    string? Player,
    string? PlayerIn,
    string? PlayerOut,
    string? Note,
    int Minute,
    int OffsetSeconds,
    DateTimeOffset RecordedAt)
{
    public static MatchEventView From(MatchEvent matchEvent)
    {
        string? side = null;
        string? player = null;
        string? playerIn = null;
        string? playerOut = null;
        string? note = null;

        try
        {
            using var document = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(matchEvent.PayloadJson) ? "{}" : matchEvent.PayloadJson);
            var root = document.RootElement;
            side = Read(root, "side");
            player = Read(root, "player");
            playerIn = Read(root, "playerIn");
            playerOut = Read(root, "playerOut");
            note = Read(root, "note");
        }
        catch (JsonException)
        {
            note = "Olay ayrıntısı okunamadı";
        }

        return new MatchEventView(
            matchEvent.MatchId,
            matchEvent.Type,
            GetTypeLabel(matchEvent.Type),
            side,
            player,
            playerIn,
            playerOut,
            note,
            matchEvent.OffsetSeconds / 60,
            matchEvent.OffsetSeconds,
            matchEvent.RecordedAt);
    }

    private static string? Read(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string GetTypeLabel(string type) => type.ToLowerInvariant() switch
    {
        "kickoff" => "Maç başladı",
        "goal" => "Gol",
        "card" => "Sarı kart",
        "sub" => "Oyuncu değişikliği",
        "halftime" => "İlk yarı bitti",
        "fulltime" => "Maç bitti",
        _ => "Maç olayı"
    };
}
