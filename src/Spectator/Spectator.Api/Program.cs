using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Spectator.Api;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5185, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(5186, lo => lo.Protocols = HttpProtocols.Http2);
});

builder.Services.AddSingleton(new EventStore(
    Path.Combine(builder.Environment.ContentRootPath, "data", "matches.json")));
builder.Services.AddSingleton<MetricsCollector>();
builder.Services.AddSignalR();
builder.Services.AddGrpc();
builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Spectator API", 
        Version = "v1",
        Description = "Event sourcing + SignalR + gRPC - Canlı maç skorları ve time-travel replay"
    });
});

var app = builder.Build();
MatchDemoSeed.Apply(app.Services.GetRequiredService<EventStore>(), app.Services.GetRequiredService<MetricsCollector>());

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Spectator API v1");
    c.RoutePrefix = "swagger";
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");

app.MapHub<MatchHub>("/hub/match");
app.MapGrpcService<MatchIngestService>();

app.MapPost("/api/matches/{matchId}/events", async (
    string matchId,
    EventDto dto,
    EventStore store,
    MetricsCollector metrics,
    IHubContext<MatchHub> hub) =>
{
    if (store.IsFinished(matchId))
    {
        return Results.Conflict(new { message = "Bu maç bitmiş ve arşivlenmiştir. Yeni olay eklenemez." });
    }

    var allowedTypes = new[] { "kickoff", "goal", "card", "sub", "halftime", "fulltime" };
    var type = dto.Type.Trim().ToLowerInvariant();
    if (!allowedTypes.Contains(type))
    {
        return Results.BadRequest(new { message = "Geçersiz olay türü." });
    }

    if (type is "goal" or "card" or "sub" && dto.Side is not "home" and not "away")
    {
        return Results.BadRequest(new { message = "Bu olay için ev sahibi veya deplasman seçilmelidir." });
    }

    var offsetSeconds = dto.OffsetSeconds ?? Math.Max(0, dto.Minute) * 60;
    var payload = dto.PayloadJson ?? JsonSerializer.Serialize(new
    {
        side = dto.Side,
        player = dto.Player,
        playerIn = dto.PlayerIn,
        playerOut = dto.PlayerOut,
        note = dto.Note
    });
    var evt = store.Append(matchId, type, payload, offsetSeconds);
    var eventView = MatchEventView.From(evt);
    var snap = store.Replay(matchId, offsetSeconds);
    
    metrics.RecordEvent(matchId, type);
    
    await hub.Clients.Group(matchId).SendAsync("event", eventView);
    await hub.Clients.Group(matchId).SendAsync("snapshot", snap);
    return Results.Ok(new { @event = eventView, snapshot = snap });
})
.WithName("PostMatchEvent")
.WithTags("Matches");

app.MapPost("/api/matches", (CreateMatchDto dto, EventStore store) =>
{
    if (string.IsNullOrWhiteSpace(dto.MatchId) ||
        string.IsNullOrWhiteSpace(dto.HomeTeam) ||
        string.IsNullOrWhiteSpace(dto.AwayTeam))
    {
        return Results.BadRequest(new { message = "Maç kodu ve takım adları zorunludur." });
    }

    var matchId = dto.MatchId.Trim();
    if (store.Exists(matchId))
    {
        return Results.Conflict(new { message = "Bu maç kodu daha önce kullanılmış. Yeni bir maç kodu girin." });
    }

    store.SetTeams(matchId, dto.HomeTeam.Trim(), dto.AwayTeam.Trim());
    return Results.Created($"/api/matches/{dto.MatchId}", new
    {
        id = matchId,
        homeTeam = dto.HomeTeam.Trim(),
        awayTeam = dto.AwayTeam.Trim()
    });
})
.WithName("CreateMatch")
.WithTags("Matches");

app.MapGet("/api/matches/{matchId}/replay", (string matchId, int at, EventStore store) =>
    Results.Ok(store.Replay(matchId, at)))
.WithName("ReplayMatch")
.WithTags("Matches");

app.MapGet("/api/matches/{matchId}/events", (string matchId, EventStore store) =>
    Results.Ok(store.Stream(matchId).Select(MatchEventView.From)))
.WithName("GetMatchEvents")
.WithTags("Matches");

app.MapGet("/api/matches", (EventStore store) =>
{
    var list = store.GetAllMatches().Select(id =>
    {
        var last = store.Stream(id).Select(e => e.OffsetSeconds).DefaultIfEmpty(0).Max();
        var snap = store.Replay(id, last);
        return new
        {
            id,
            homeTeam = snap.HomeTeam,
            awayTeam = snap.AwayTeam,
            home = snap.Home,
            away = snap.Away,
            events = snap.AppliedEvents.Count,
            isFinished = store.IsFinished(id)
        };
    });
    return Results.Ok(list);
})
.WithName("GetAllMatches")
.WithTags("Matches");

app.MapDelete("/api/matches/{matchId}", (string matchId, EventStore store) =>
{
    store.Clear(matchId);
    return Results.NoContent();
})
.WithName("DeleteMatch")
.WithTags("Matches");

app.MapGet("/api/metrics", (MetricsCollector metrics) =>
    Results.Ok(metrics.GetSnapshot()))
.WithName("GetMetrics")
.WithTags("Monitoring");

app.Run();

public sealed record CreateMatchDto(string MatchId, string HomeTeam, string AwayTeam);

public sealed record EventDto(
    string Type,
    int Minute = 0,
    string? Side = null,
    string? Player = null,
    string? PlayerIn = null,
    string? PlayerOut = null,
    string? Note = null,
    int? OffsetSeconds = null,
    string? PayloadJson = null);
