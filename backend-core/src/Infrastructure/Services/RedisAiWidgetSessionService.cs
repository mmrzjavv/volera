using Core.Application.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Services;

public class RedisAiWidgetSessionService : IAiWidgetSessionService
{
    private const string KeyPrefix = "ai_widget_session:";
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);
    private readonly IDatabase _db;

    public RedisAiWidgetSessionService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<string> CreateSessionAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "").Replace("/", "").TrimEnd('=');
        var key = KeyPrefix + token;
        await _db.StringSetAsync(key, branchId.ToString(), SessionTtl);
        return token;
    }

    public async Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var key = KeyPrefix + token.Trim();
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return Guid.TryParse(value.ToString(), out var branchId) ? branchId : null;
    }
}
