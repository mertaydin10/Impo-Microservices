using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Spectator.Api.GrpcContracts;

namespace Spectator.Api;

public sealed class MatchIngestService : MatchIngest.MatchIngestBase
{
    private readonly EventStore _store;
    private readonly IHubContext<MatchHub> _hub;

    public MatchIngestService(EventStore store, IHubContext<MatchHub> hub)
    {
        _store = store;
        _hub = hub;
    }

    public override async Task<Ack> Push(MatchEventMsg request, ServerCallContext context)
    {
        var evt = _store.Append(request.MatchId, request.Type, request.PayloadJson, request.OffsetSeconds);
        var snap = _store.Replay(request.MatchId, request.OffsetSeconds);
        await _hub.Clients.Group(request.MatchId).SendAsync("event", evt, context.CancellationToken);
        await _hub.Clients.Group(request.MatchId).SendAsync("snapshot", snap, context.CancellationToken);
        return new Ack { Ok = true };
    }
}
