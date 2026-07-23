using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SystemMessageRepository : Repository<SystemMessage>, ISystemMessageRepository
{
    public SystemMessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SystemMessage>> GetActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(m => m.IsActive && (m.ExpiresAt == null || m.ExpiresAt > utcNow))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

public class SystemMessageReadRepository : Repository<SystemMessageRead>, ISystemMessageReadRepository
{
    public SystemMessageReadRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlySet<Guid>> GetReadMessageIdsForUserAsync(Guid userId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        var ids = await _dbSet
            .AsNoTracking()
            .Where(r => r.UserId == userId && messageIds.Contains(r.MessageId))
            .Select(r => r.MessageId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task<bool> HasReadAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId, cancellationToken);
    }
}

