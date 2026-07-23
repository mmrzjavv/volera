using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SavedMessageRepository : Repository<SavedMessage>, ISavedMessageRepository
{
    public SavedMessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SavedMessage?> GetByUserAndMessageIdAsync(Guid userId, Guid messageId)
    {
        return await _context.SavedMessages
            .FirstOrDefaultAsync(sm => sm.UserId == userId && sm.MessageId == messageId);
    }

    public async Task<IEnumerable<SavedMessage>> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        return await _context.SavedMessages
            .Include(sm => sm.Message)
            .Where(sm => sm.UserId == userId)
            .OrderByDescending(sm => sm.SavedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId)
    {
        return await _context.SavedMessages
            .CountAsync(sm => sm.UserId == userId);
    }

    public async Task<IEnumerable<Guid>> GetSavedMessageIdsAsync(Guid userId, IEnumerable<Guid> messageIds)
    {
        return await _context.SavedMessages
            .Where(sm => sm.UserId == userId && messageIds.Contains(sm.MessageId))
            .Select(sm => sm.MessageId)
            .ToListAsync();
    }
}
