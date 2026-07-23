using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MessageReactionRepository : Repository<MessageReaction>, IMessageReactionRepository
{
    public MessageReactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MessageReaction?> GetByMessageAndUserAsync(Guid messageId, Guid userId)
    {
        return await _context.Set<MessageReaction>()
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
    }

    public async Task<MessageReaction?> GetByMessageAndSupportUserAsync(Guid messageId, Guid supportUserId)
    {
        return await _context.Set<MessageReaction>()
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.SupportUserId == supportUserId);
    }

    public async Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(IEnumerable<Guid> messageIds)
    {
        var ids = messageIds.ToList();
        if (ids.Count == 0) return Array.Empty<MessageReaction>();

        return await _context.Set<MessageReaction>()
            .Where(r => ids.Contains(r.MessageId))
            .Include(r => r.User)
            .Include(r => r.SupportUser)
            .ToListAsync();
    }
}

