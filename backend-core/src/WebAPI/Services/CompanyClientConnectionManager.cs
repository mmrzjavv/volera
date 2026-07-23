using System.Collections.Concurrent;
using Core.Application.Interfaces;

namespace WebAPI.Services;

public class CompanyClientConnectionManager : ICompanyClientConnectionManager
{
    private readonly ConcurrentDictionary<string, Guid> _connectionToUserId = new();

    public void RegisterConnection(string connectionId, Guid clientUserId)
    {
        _connectionToUserId.TryAdd(connectionId, clientUserId);
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
