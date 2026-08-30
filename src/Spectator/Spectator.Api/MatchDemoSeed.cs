namespace Spectator.Api;

public static class MatchDemoSeed
{
    public static void Apply(EventStore store, MetricsCollector metrics)
    {
        if (store.GetAllMatches().Count > 0)
        {
            return;
        }

        SeedDerby(store, metrics);
        SeedEurope(store, metrics);
        SeedFriendly(store, metrics);
    }

    private static void SeedDerby(EventStore store, MetricsCollector metrics)
    {
        const string id = "gs-fb";
        store.SetTeams(id, "Galatasaray", "Fenerbahçe");

        Push(store, metrics, id, "kickoff", 0, """{"note":"Derbi başladı"}""");
        Push(store, metrics, id, "goal", 12 * 60, """{"side":"home","player":"Icardi","minute":12}""");
        Push(store, metrics, id, "card", 24 * 60, """{"side":"away","player":"Fred","color":"yellow"}""");
        Push(store, metrics, id, "goal", 33 * 60, """{"side":"away","player":"Džeko","minute":33}""");
        Push(store, metrics, id, "goal", 41 * 60, """{"side":"home","player":"Mertens","minute":41}""");
        Push(store, metrics, id, "sub", 46 * 60, """{"side":"away","playerOut":"Tadić","playerIn":"Szymanski"}""");
        Push(store, metrics, id, "goal", 67 * 60, """{"side":"home","player":"Yunus Akgün","minute":67}""");
        Push(store, metrics, id, "card", 78 * 60, """{"side":"home","player":"Torreira","color":"yellow"}""");
        Push(store, metrics, id, "goal", 89 * 60, """{"side":"away","player":"Talisca","minute":89}""");
    }

    private static void SeedEurope(EventStore store, MetricsCollector metrics)
    {
        const string id = "aja-int";
        store.SetTeams(id, "Ajax", "Inter");
        Push(store, metrics, id, "kickoff", 0, """{"note":"Şampiyonlar Ligi"}""");
        Push(store, metrics, id, "goal", 9 * 60, """{"side":"away","player":"Lautaro","minute":9}""");
        Push(store, metrics, id, "goal", 55 * 60, """{"side":"away","player":"Thuram","minute":55}""");
        Push(store, metrics, id, "sub", 70 * 60, """{"side":"home","playerOut":"Berghuis","playerIn":"Godts"}""");
    }

    private static void SeedFriendly(EventStore store, MetricsCollector metrics)
    {
        const string id = "demo";
        store.SetTeams(id, "Anadolu", "Karadeniz");
        Push(store, metrics, id, "kickoff", 0, """{"note":"Hazırlık maçı"}""");
        Push(store, metrics, id, "goal", 18 * 60, """{"side":"home","player":"Yılmaz","minute":18}""");
        Push(store, metrics, id, "goal", 51 * 60, """{"side":"home","player":"Şahin","minute":51}""");
        Push(store, metrics, id, "card", 63 * 60, """{"side":"away","player":"Kaya","color":"yellow"}""");
    }

    private static void Push(EventStore store, MetricsCollector metrics, string matchId, string type, int offset, string json)
    {
        store.Append(matchId, type, json, offset);
        metrics.RecordEvent(matchId, type);
    }
}
