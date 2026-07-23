using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SuggestedPostRepository : Repository<SuggestedPost>, ISuggestedPostRepository
{
    public SuggestedPostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SuggestedPost>> GetByChannelAsync(Guid channelId, SuggestedPostStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking()
            .Include(s => s.FromUser)
            .Where(s => s.ChannelId == channelId);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);
        return await query.OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<SuggestedPost?> GetByIdWithChannelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Channel)
                .ThenInclude(c => c.Members)
            .Include(s => s.FromUser)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
