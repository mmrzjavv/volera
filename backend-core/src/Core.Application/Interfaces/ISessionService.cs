using Core.Application.DTOs;
using Core.Domain.Entities;

namespace Core.Application.Interfaces;

public interface ISessionService
{
    /// <summary>
    /// Gets session by id (cache-aside: Redis then PostgreSQL). Returns null if not found or revoked.
    /// </summary>
    Task<SessionInfoDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates LastActivityAt in Redis and PostgreSQL.
    /// </summary>
    Task TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes session to Redis after it has been persisted to PostgreSQL (call after AddAsync + SaveChanges).
    /// </summary>
    Task SaveSessionToCacheAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates session's app version in cache and optionally in DB.
    /// </summary>
    Task UpdateSessionAppVersionAsync(Guid sessionId, string appVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the session (PostgreSQL and Redis).
    /// </summary>
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes session from Redis (e.g. after revoke). Call after updating session in PG.
    /// </summary>
    Task InvalidateSessionCacheAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active sessions for the user (for "You are logged in from…"). Optionally exclude one session (e.g. current).
    /// </summary>
    Task<IReadOnlyList<SessionInfoDto>> GetActiveSessionsForUserAsync(Guid userId, Guid? excludeSessionId, CancellationToken cancellationToken = default);
}
