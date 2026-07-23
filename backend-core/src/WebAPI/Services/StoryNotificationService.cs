using Core.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class StoryNotificationService : IStoryNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IConnectionManager _connectionManager;

    public StoryNotificationService(IHubContext<ChatHub> hubContext, IConnectionManager connectionManager)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
    }

    public async Task NotifyStoryCreated(Guid authorUserId, Guid storyId, IEnumerable<Guid> contactUserIds)
    {
        var payload = new { authorUserId, storyId };
        foreach (var userId in contactUserIds.Distinct())
        {
            var connections = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connections.Count == 0) continue;
            await _hubContext.Clients.Clients(connections).SendAsync("StoryCreated", payload);
        }
    }

    public async Task NotifyStoryDeleted(Guid authorUserId, Guid storyId, IEnumerable<Guid> contactUserIds)
    {
        var payload = new { authorUserId, storyId };
        foreach (var userId in contactUserIds.Distinct())
        {
            var connections = _connectionManager.GetConnectionsForUser(userId.ToString());
            if (connections.Count == 0) continue;
            await _hubContext.Clients.Clients(connections).SendAsync("StoryDeleted", payload);
        }
    }
}
