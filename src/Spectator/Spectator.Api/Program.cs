using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Spectator.Api;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5185, lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(5186, lo => lo.Protocols = HttpProtocols.Http2);
});

builder.Services.AddSingleton<EventStore>();
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
    var evt = store.Append(matchId, dto.Type, dto.PayloadJson ?? "{}", dto.OffsetSeconds);
    var snap = store.Replay(matchId, dto.OffsetSeconds);
    
    metrics.RecordEvent(matchId, dto.Type);
    
    await hub.Clients.Group(matchId).SendAsync("event", evt);
    await hub.Clients.Group(matchId).SendAsync("snapshot", snap);
    return Results.Ok(new { evt, snap });
})
.WithName("PostMatchEvent")
.WithTags("Matches");

app.MapGet("/api/matches/{matchId}/replay", (string matchId, int at, EventStore store) =>
    Results.Ok(store.Replay(matchId, at)))
.WithName("ReplayMatch")
.WithTags("Matches");

app.MapGet("/api/matches/{matchId}/events", (string matchId, EventStore store) =>
    Results.Ok(store.Stream(matchId)))
.WithName("GetMatchEvents")
.WithTags("Matches");

app.MapGet("/api/matches", (EventStore store) =>
    Results.Ok(store.GetAllMatches()))
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

public sealed record EventDto(string Type, int OffsetSeconds, string? PayloadJson);
