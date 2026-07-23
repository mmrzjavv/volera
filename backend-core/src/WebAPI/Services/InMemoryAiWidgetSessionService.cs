using System.Collections.Concurrent;
using Core.Application.Interfaces;

namespace WebAPI.Services;

/// <summary>
/// In-memory fallback when Redis is not configured. Sessions are lost on restart.
/// </summary>
public class InMemoryAiWidgetSessionService : IAiWidgetSessionService
{
    private static readonly ConcurrentDictionary<string, (Guid BranchId, DateTime Expires)> _store = new();
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);

    public Task<string> CreateSessionAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "").Replace("/", "").TrimEnd('=');
        _store[token] = (branchId, DateTime.UtcNow.Add(SessionTtl));
        return Task.FromResult(token);
    }

    public Task<Guid?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return Task.FromResult<Guid?>(null);
        if (!_store.TryGetValue(token.Trim(), out var entry)) return Task.FromResult<Guid?>(null);
        if (DateTime.UtcNow > entry.Expires)
        {
            _store.TryRemove(token.Trim(), out _);
            return Task.FromResult<Guid?>(null);
        }
        return Task.FromResult<Guid?>(entry.BranchId);
    }
}
