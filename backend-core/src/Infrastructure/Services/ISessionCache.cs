using Core.Application.DTOs;

namespace Infrastructure.Services;

/// <summary>
/// Abstraction for session data in Redis. When Redis is not configured, use <see cref="NullSessionCache"/>.
/// </summary>
public interface ISessionCache
{
    Task<SessionInfoDto?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task SetAsync(Guid sessionId, SessionInfoDto data, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
