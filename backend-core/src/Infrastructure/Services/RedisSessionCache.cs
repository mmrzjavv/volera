using System.Text.Json;
using Core.Application.DTOs;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisSessionCache : ISessionCache
{
    private readonly IDatabase _database;
    private const string KeyPrefix = "session:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(8);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RedisSessionCache(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    private static string Key(Guid sessionId) => KeyPrefix + sessionId.ToString("N");

    public async Task<SessionInfoDto?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = Key(sessionId);
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue || value.IsNullOrEmpty) return null;
        try
        {
            return JsonSerializer.Deserialize<SessionInfoDto>(value!, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync(Guid sessionId, SessionInfoDto data, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = Key(sessionId);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await _database.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var key = Key(sessionId);
        await _database.KeyDeleteAsync(key);
    }
}
