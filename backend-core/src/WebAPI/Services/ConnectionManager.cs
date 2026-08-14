using System.Collections.Concurrent;
using Core.Application.Interfaces;

namespace WebAPI.Services;

/// <summary>
/// Node-local, concurrency-safe connection manager used by SignalR hubs and
/// notification services. This implementation is DI-friendly and avoids
/// static mutable state while still leveraging efficient concurrent collections.
/// </summary>
public class ConnectionManager : IConnectionManager
{
    // Store connection ID to user ID mapping
    private readonly ConcurrentDictionary<string, string> _connectionToUserId = new();
    // Store user ID to connection IDs mapping (one user can have multiple connections)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userIdToConnections = new();

    public void RegisterConnection(string connectionId, string userId)
    {
        _connectionToUserId.TryAdd(connectionId, userId);

        _userIdToConnections.AddOrUpdate(
            userId,
            _ =>
            {
                var dict = new ConcurrentDictionary<string, byte>();
                dict.TryAdd(connectionId, 0);
                return dict;
            },
            (_, existing) =>
            {
                existing.TryAdd(connectionId, 0);
                return existing;
            });
    }

    public void UnregisterConnection(string connectionId)
    {
        if (_connectionToUserId.TryRemove(connectionId, out var userId))
        {
            if (_userIdToConnections.TryGetValue(userId, out var connections))
            {
                connections.TryRemove(connectionId, out _);
                if (connections.IsEmpty)
                {
                    _userIdToConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    public IReadOnlyList<string> GetConnectionsForUser(string userId)
    {
        if (_userIdToConnections.TryGetValue(userId, out var connections))
        {
            return connections.Keys.ToList();
        }
        return Array.Empty<string>();
    }
}
