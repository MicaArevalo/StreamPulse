using Microsoft.AspNetCore.SignalR;

namespace StreamPulse.Api.Hubs;

public sealed class MetricsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "metrics");
        await base.OnConnectedAsync();
    }
}
