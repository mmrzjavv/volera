using Core.Application.Interfaces;
using System.Collections.Concurrent;

namespace WebAPI.Services;

public class OnlineUserService : IOnlineUserService
{
    private static readonly ConcurrentDictionary<Guid, int> _userConnectionCount = new();

    public Task<bool> IsUserOnline(Guid userId)
    {
        return Task.FromResult(_userConnectionCount.ContainsKey(userId) && _userConnectionCount[userId] > 0);
    }

    public Task<IEnumerable<Guid>> GetOnlineUserIds()
    {
        return Task.FromResult(_userConnectionCount.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key));
    }

    public Task UserConnected(Guid userId)
    {
        _userConnectionCount.AddOrUpdate(userId, 1, (k, v) => v + 1);
        return Task.CompletedTask;
    }

    public Task UserDisconnected(Guid userId)
    {
        _userConnectionCount.AddOrUpdate(userId, 0, (k, v) => v - 1);
        if (_userConnectionCount[userId] <= 0)
        {
            _userConnectionCount.TryRemove(userId, out _);
        }
        return Task.CompletedTask;
    }
}