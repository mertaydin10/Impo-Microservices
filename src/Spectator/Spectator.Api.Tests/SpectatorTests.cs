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
