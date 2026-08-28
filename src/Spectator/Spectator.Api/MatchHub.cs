using Microsoft.AspNetCore.SignalR;

namespace Spectator.Api;

public sealed class MatchHub : Hub
{
    public Task JoinMatch(string matchId) => Groups.AddToGroupAsync(Context.ConnectionId, matchId);
}
