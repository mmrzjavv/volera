using Core.Application.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Services;

/// <summary>
/// Distributed implementation of <see cref="IOnlineUserService"/> backed by Redis.
/// Designed to be safe under high concurrency and across many application instances.
/// </summary>
public class RedisOnlineUserService : IOnlineUserService
{
    private readonly IDatabase _database;
    private const string OnlineUsersHashKey = "presence:online-users-count";

    public RedisOnlineUserService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<bool> IsUserOnline(Guid userId)
    {
        var value = await _database.HashGetAsync(OnlineUsersHashKey, userId.ToString());
        if (!value.HasValue)
        {
            return false;
        }

        if (long.TryParse(value.ToString(), out var count))
        {
            return count > 0;
        }

        return false;
    }

    public async Task<IEnumerable<Guid>> GetOnlineUserIds()
    {
        // NOTE: This returns all online users across the cluster.
        // For extremely large user bases, prefer adding a batched "AreUsersOnline" API instead.
        var entries = await _database.HashGetAllAsync(OnlineUsersHashKey);
        return entries
            .Where(e =>
            {
                if (!e.Value.HasValue) return false;
                return long.TryParse(e.Value.ToString(), out var count) && count > 0;
            })
            .Select(e => Guid.Parse(e.Name!));
    }

    public async Task UserConnected(Guid userId)
    {
        // Increment the connection count for this user in a concurrency-safe way.
        await _database.HashIncrementAsync(OnlineUsersHashKey, userId.ToString(), 1);
    }

    public async Task UserDisconnected(Guid userId)
    {
        var newValue = await _database.HashDecrementAsync(OnlineUsersHashKey, userId.ToString(), 1);

        if (newValue <= 0)
        {
            // Clean up entry when the count drops to zero or below.
            await _database.HashDeleteAsync(OnlineUsersHashKey, userId.ToString());
        }
    }
}

