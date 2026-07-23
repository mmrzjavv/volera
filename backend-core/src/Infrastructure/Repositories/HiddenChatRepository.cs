using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class HiddenChatRepository : IHiddenChatRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<HiddenChat> _dbSet;

    public HiddenChatRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<HiddenChat>();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AnyAsync(h => h.UserId == userId && h.OtherUserId == otherUserId, cancellationToken);
    }

    public async Task AddAsync(HiddenChat hiddenChat, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(hiddenChat, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetHiddenUserIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ids = await _dbSet
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .Select(h => h.OtherUserId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
