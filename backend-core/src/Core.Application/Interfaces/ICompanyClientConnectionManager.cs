namespace Core.Application.Interfaces;

/// <summary>
/// Maps SignalR connection ID to company client user ID for real-time delivery.
/// </summary>
public interface ICompanyClientConnectionManager
{
    void RegisterConnection(string connectionId, Guid clientUserId);
    void UnregisterConnection(string connectionId);
    Guid? GetUserIdForConnection(string connectionId);
}
