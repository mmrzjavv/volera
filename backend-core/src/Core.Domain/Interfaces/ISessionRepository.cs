using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ISessionRepository : IRepository<Session>
{
    Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default);
    Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Session?> GetOldestActiveSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(Guid userId, Guid? excludeSessionId, CancellationToken cancellationToken = default);
}
