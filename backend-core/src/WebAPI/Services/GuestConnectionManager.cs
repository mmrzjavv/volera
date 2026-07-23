using System.Collections.Concurrent;
using Core.Application.Interfaces;

namespace WebAPI.Services;

/// <summary>
/// Dedicated SignalR hub for guests; auth via guest token in query string; isolates guest real-time flow from JWT-only ChatHub.
/// Maps connection ID to guest UserId so SendMessage can use the correct sender and so IConnectionManager has guest connections for receiving replies.
/// </summary>
public class GuestConnectionManager : IGuestConnectionManager
{
    private readonly ConcurrentDictionary<string, Guid> _connectionToUserId = new();

    public void RegisterConnection(string connectionId, Guid guestUserId)
    {
        _connectionToUserId.TryAdd(connectionId, guestUserId);
    }

    public void UnregisterConnection(string connectionId)
    {
        _connectionToUserId.TryRemove(connectionId, out _);
    }

    public Guid? GetUserIdForConnection(string connectionId)
    {
        return _connectionToUserId.TryGetValue(connectionId, out var userId) ? userId : null;
    }
}
