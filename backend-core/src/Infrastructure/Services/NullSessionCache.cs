using Core.Application.DTOs;

namespace Infrastructure.Services;

/// <summary>
/// No-op cache when Redis is not configured. Get always returns null so SessionService falls back to PostgreSQL.
/// </summary>
public class NullSessionCache : ISessionCache
{
    public Task<SessionInfoDto?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<SessionInfoDto?>(null);

    public Task SetAsync(Guid sessionId, SessionInfoDto data, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
