using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SessionRepository : Repository<Session>, ISessionRepository
{
    public SessionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Session?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshTokenHash)) return null;
        return await _context.Sessions
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);
    }

    public async Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .CountAsync(s => s.UserId == userId && s.RevokedAt == null, cancellationToken);
    }

    public async Task<Session?> GetOldestActiveSessionByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .OrderBy(s => s.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Session>> GetActiveSessionsByUserIdAsync(Guid userId, Guid? excludeSessionId, CancellationToken cancellationToken = default)
    {
        var query = _context.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null);
        if (excludeSessionId.HasValue)
            query = query.Where(s => s.Id != excludeSessionId.Value);
        return await query
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
    }
}
