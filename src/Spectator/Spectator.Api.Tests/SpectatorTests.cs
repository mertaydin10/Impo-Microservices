using Spectator.Api;

namespace Spectator.Api.Tests;

public class EventStoreTests
{
    [Fact]
    public void Append_CreatesEvent_ReturnsEvent()
    {
        var store = new EventStore();
        var evt = store.Append("match-1", "goal", "{\"side\":\"home\"}", 45);
        
        Assert.NotNull(evt);
        Assert.Equal("match-1", evt.MatchId);
        Assert.Equal("goal", evt.Type);
        Assert.Equal(45, evt.OffsetSeconds);
    }

    [Fact]
    public void Stream_ReturnsAllEvents_ForMatch()
    {
        var store = new EventStore();
        store.Append("match-1", "goal", "{}", 10);
        store.Append("match-1", "goal", "{}", 20);
        store.Append("match-2", "goal", "{}", 15);

        var events = store.Stream("match-1");
        
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void Replay_CalculatesCorrectScore()
    {
        var store = new EventStore();
        store.Append("match-1", "goal", "{\"side\":\"home\"}", 10);
        store.Append("match-1", "goal", "{\"side\":\"away\"}", 20);
        store.Append("match-1", "goal", "{\"side\":\"home\"}", 30);

        var snapshot = store.Replay("match-1", 25);
        
        Assert.Equal(1, snapshot.Home);
        Assert.Equal(1, snapshot.Away);
    }

    [Fact]
    public void Replay_OnlyIncludesEventsUpToTime()
    {
        var store = new EventStore();
        store.Append("match-1", "goal", "{\"side\":\"home\"}", 10);
        store.Append("match-1", "goal", "{\"side\":\"home\"}", 30);
        store.Append("match-1", "goal", "{\"side\":\"home\"}", 50);

        var snapshot = store.Replay("match-1", 35);
        
        Assert.Equal(2, snapshot.Home);
        Assert.Equal(2, snapshot.AppliedEvents.Count);
    }

    [Fact]
    public void GetAllMatches_ReturnsMatchIds()
    {
        var store = new EventStore();
        store.Append("match-1", "goal", "{}", 10);
        store.Append("match-2", "goal", "{}", 10);

        var matches = store.GetAllMatches();
        
        Assert.Equal(2, matches.Count);
        Assert.Contains("match-1", matches);
        Assert.Contains("match-2", matches);
    }

    [Fact]
    public void Clear_RemovesMatch()
    {
        var store = new EventStore();
        store.Append("match-1", "goal", "{}", 10);
        
        store.Clear("match-1");
        var events = store.Stream("match-1");
        
        Assert.Empty(events);
    }

    [Fact]
    public void Append_WhenMatchIsFinished_RejectsNewEvent()
    {
        var store = new EventStore();
        store.Append("match-1", "fulltime", "{}", 90 * 60);

        var error = Assert.Throws<InvalidOperationException>(() =>
            store.Append("match-1", "goal", """{"side":"home"}""", 91 * 60));

        Assert.Equal("Biten maça yeni olay eklenemez.", error.Message);
        Assert.True(store.IsFinished("match-1"));
        Assert.Single(store.Stream("match-1"));
    }

    [Fact]
    public void Clear_WhenMatchIsFinished_AllowsFreshMatch()
    {
        var store = new EventStore();
        store.Append("match-1", "fulltime", "{}", 90 * 60);

        store.Clear("match-1");
        var newEvent = store.Append("match-1", "kickoff", "{}", 0);

        Assert.False(store.IsFinished("match-1"));
        Assert.Equal("kickoff", newEvent.Type);
    }

    [Fact]
    public void MatchEventView_RedCard_UsesReadableLabel()
    {
        var view = MatchEventView.From(new MatchEvent(
            "match-1",
            "card",
            """{"side":"home","player":"S. Yılmaz","cardKind":"red"}""",
            38 * 60,
            DateTimeOffset.UtcNow));

        Assert.Equal("red", view.CardKind);
        Assert.Equal("Kırmızı kart", view.TypeLabel);
        Assert.Equal("S. Yılmaz", view.Player);
        Assert.Equal(38, view.Minute);
    }

    [Fact]
    public void PersistentStore_AfterRestart_KeepsFinishedMatchReadOnly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"spectator-{Guid.NewGuid():N}.json");
        try
        {
            var firstInstance = new EventStore(path);
            firstInstance.SetTeams("match-1", "Ev", "Deplasman");
            firstInstance.Append("match-1", "fulltime", "{}", 90 * 60);

            var restartedInstance = new EventStore(path);

            Assert.True(restartedInstance.Exists("match-1"));
            Assert.True(restartedInstance.IsFinished("match-1"));
            Assert.Throws<InvalidOperationException>(() =>
                restartedInstance.Append("match-1", "goal", """{"side":"home"}""", 91 * 60));
        }
        finally
        {
            File.Delete(path);
            File.Delete($"{path}.tmp");
        }
    }
}

public class MetricsCollectorTests
{
    [Fact]
    public void RecordEvent_IncrementsCounters()
    {
        var metrics = new MetricsCollector();
        metrics.RecordEvent("match-1", "goal");
        metrics.RecordEvent("match-1", "goal");
        metrics.RecordEvent("match-2", "card");

        var snapshot = metrics.GetSnapshot();
        
        Assert.Equal(3, snapshot.TotalEvents);
        Assert.Equal(2, snapshot.ActiveMatches);
        Assert.Equal(2, snapshot.TotalEventsByType["goal"]);
        Assert.Equal(1, snapshot.TotalEventsByType["card"]);
    }

    [Fact]
    public void GetSnapshot_ReturnsUptime()
    {
        var metrics = new MetricsCollector();
        Thread.Sleep(100);
        
        var snapshot = metrics.GetSnapshot();
        
        Assert.True(snapshot.UptimeSeconds > 0);
    }
}
