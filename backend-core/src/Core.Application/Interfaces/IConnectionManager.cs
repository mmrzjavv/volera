namespace Core.Application.Interfaces;

/// <summary>
/// Abstraction over connection management for real-time clients (e.g. SignalR).
/// Implementations are typically node-local but concurrency-safe, and can be
/// swapped or extended to support different backplanes.
/// </summary>
public interface IConnectionManager
{
    void RegisterConnection(string connectionId, string userId);
    void UnregisterConnection(string connectionId);
    IReadOnlyList<string> GetConnectionsForUser(string userId);
}

