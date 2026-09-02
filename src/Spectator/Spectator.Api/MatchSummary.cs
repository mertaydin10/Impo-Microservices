using System.Text;

namespace Spectator.Api;

public static class MatchSummary
{
    public static string Build(EventStore store, string matchId)
    {
        var last = store.Stream(matchId).Select(e => e.OffsetSeconds).DefaultIfEmpty(0).Max();
        var snap = store.Replay(matchId, last);
        var lines = new StringBuilder();
        lines.AppendLine($"{snap.HomeTeam} {snap.Home}-{snap.Away} {snap.AwayTeam}");
        if (store.IsFinished(matchId))
        {
            lines.AppendLine("Durum: Bitti");
        }
        else
        {
            lines.AppendLine("Durum: Canlı");
        }

        foreach (var view in store.Stream(matchId).Select(MatchEventView.From).OrderBy(e => e.OffsetSeconds))
        {
            var detail = view.Type switch
            {
                "goal" => string.Join(" · ", new[] { SideName(snap, view.Side), view.Player }.Where(v => !string.IsNullOrWhiteSpace(v))),
                "card" => string.Join(" · ", new[] { SideName(snap, view.Side), view.Player }.Where(v => !string.IsNullOrWhiteSpace(v))),
                "sub" => string.Join(" · ", new[] { SideName(snap, view.Side), $"{view.PlayerOut} → {view.PlayerIn}".Trim(' ', '→') }.Where(v => !string.IsNullOrWhiteSpace(v))),
                _ => view.Note ?? ""
            };
            lines.AppendLine(string.IsNullOrWhiteSpace(detail)
                ? $"{view.Minute}' {view.TypeLabel}"
                : $"{view.Minute}' {view.TypeLabel} — {detail}");
        }

        return lines.ToString().TrimEnd();
    }

    private static string? SideName(MatchSnapshot snap, string? side) =>
        side == "away" ? snap.AwayTeam : side == "home" ? snap.HomeTeam : null;
}
