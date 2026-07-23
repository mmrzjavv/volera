using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserLimitOverrideRepository : Repository<UserLimitOverride>, IUserLimitOverrideRepository
{
    public UserLimitOverrideRepository(ApplicationDbContext context) : base(context) { }

    public async Task<UserLimitOverride?> GetAsync(Guid userId, string limitKey, CancellationToken cancellationToken = default)
    {
        return await _context.UserLimitOverrides
            .FirstOrDefaultAsync(u => u.UserId == userId && u.LimitKey == limitKey, cancellationToken);
    }

    public async Task<IEnumerable<UserLimitOverride>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserLimitOverrides
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
