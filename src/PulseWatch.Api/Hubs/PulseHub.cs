using Microsoft.AspNetCore.SignalR;

namespace PulseWatch.Api.Hubs;

public sealed class PulseHub : Hub
{
    public Task JoinProject(Guid projectId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"proj:{projectId}");

    public Task LeaveProject(Guid projectId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"proj:{projectId}");
}
