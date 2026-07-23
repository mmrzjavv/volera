using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Services;

namespace Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly ISessionCache _cache;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(8);

    public SessionService(ISessionCache cache, ISessionRepository sessionRepository, IUnitOfWork unitOfWork)
    {
        _cache = cache;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SessionInfoDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(sessionId, cancellationToken);
        if (cached != null)
            return cached;

        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || !session.IsActive)
            return null;

        var dto = MapToDto(session);
        await _cache.SetAsync(sessionId, dto, SessionTtl, cancellationToken);
        return dto;
    }

    public async Task TouchSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || !session.IsActive) return;

        session.Touch();
        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(session);
        await _cache.SetAsync(sessionId, dto, SessionTtl, cancellationToken);
    }

    public async Task SaveSessionToCacheAsync(Session session, CancellationToken cancellationToken = default)
    {
        if (session == null || !session.IsActive) return;
        var dto = MapToDto(session);
        await _cache.SetAsync(session.Id, dto, SessionTtl, cancellationToken);
    }

    public async Task UpdateSessionAppVersionAsync(Guid sessionId, string appVersion, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || string.IsNullOrWhiteSpace(appVersion)) return;

        session.UpdateAppVersion(appVersion);
        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(session);
        await _cache.SetAsync(sessionId, dto, SessionTtl, cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return;

        session.Revoke();
        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync();
        await _cache.RemoveAsync(sessionId, cancellationToken);
    }

    public async Task InvalidateSessionCacheAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<SessionInfoDto>> GetActiveSessionsForUserAsync(Guid userId, Guid? excludeSessionId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, excludeSessionId, cancellationToken);
        return sessions.Select(MapToDto).ToList();
    }

    private static SessionInfoDto MapToDto(Session s)
    {
        return new SessionInfoDto
        {
            Id = s.Id,
            UserId = s.UserId,
            DeviceType = s.DeviceType,
            Browser = s.Browser,
            OS = s.OS,
            Location = s.Location,
            LoginAt = s.LoginAt,
            LastActivityAt = s.LastActivityAt,
            AppVersion = s.AppVersion,
            IsRevoked = s.RevokedAt != null
        };
    }
}
