using System.Collections.Generic;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GuestRepository : Repository<Guest>, IGuestRepository
{
    public GuestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Guest?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tokenHash)) return null;
        return await _context.Guests
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.SessionTokenHash == tokenHash, cancellationToken);
    }

    public async Task<Guest?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Guests
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, Guest>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, Guest>();
        var list = await _context.Guests
            .Include(g => g.User)
            .Where(g => idList.Contains(g.UserId))
            .ToListAsync(cancellationToken);
        return list.ToDictionary(g => g.UserId);
    }
}
