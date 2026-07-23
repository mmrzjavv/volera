namespace Core.Application.Interfaces;

/// <summary>
/// Maps guest SignalR connection IDs to guest user IDs so the hub can identify the sender.
/// Also used so the notification pipeline can push replies to guest connections (via IConnectionManager with same userId).
/// </summary>
public interface IGuestConnectionManager
{
    void RegisterConnection(string connectionId, Guid guestUserId);
    void UnregisterConnection(string connectionId);
    Guid? GetUserIdForConnection(string connectionId);
}
