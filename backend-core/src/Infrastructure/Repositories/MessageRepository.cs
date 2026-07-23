using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetConversationAsync(Guid userId1, Guid userId2, int limit, DateTime? before)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1));

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        return messages.OrderBy(m => m.SentAt);
    }

    public async Task<IEnumerable<Message>> GetGroupMessagesAsync(Guid groupId, int limit, DateTime? before)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Where(m => m.GroupId == groupId);

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync();

        return messages.OrderBy(m => m.SentAt);
    }

    public async Task<Message?> GetBySenderAndClientMessageIdAsync(Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.SenderId == senderId && m.ClientMessageId == clientMessageId, cancellationToken);
    }

    public async Task<IEnumerable<Message>> SyncConversationAsync(Guid userId1, Guid userId2, DateTime? afterSentAt, Guid? afterId, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1));

        if (afterSentAt.HasValue)
        {
            var afterIdValue = afterId ?? Guid.Empty;
            query = query.Where(m =>
                m.SentAt > afterSentAt.Value ||
                (m.SentAt == afterSentAt.Value && m.Id > afterIdValue));
        }

        return await query
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> SyncGroupMessagesAsync(Guid groupId, DateTime? afterSentAt, Guid? afterId, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Where(m => m.GroupId == groupId);

        if (afterSentAt.HasValue)
        {
            var afterIdValue = afterId ?? Guid.Empty;
            query = query.Where(m =>
                m.SentAt > afterSentAt.Value ||
                (m.SentAt == afterSentAt.Value && m.Id > afterIdValue));
        }

        return await query
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid userId)
    {
        return await _dbSet
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .GroupBy(m => m.SenderId)
            .Select(g => new { SenderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SenderId, x => x.Count);
    }

    public async Task<IEnumerable<Core.Domain.Models.RecentChatResult>> GetRecentChatsAsync(Guid userId)
    {
        // DMs
        // Use AsNoTracking for read-only queries to improve performance
        var dmsQuery = _dbSet.AsNoTracking()
            .Where(m => m.GroupId == null && (m.SenderId == userId || m.ReceiverId == userId))
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new Core.Domain.Models.RecentChatResult
            {
                OtherUserId = g.Key,
                GroupId = null,
                LastMessage = g.OrderByDescending(m => m.SentAt).FirstOrDefault(),
                UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
            });

        // Groups
        var groupsQuery = _context.Set<GroupMember>().AsNoTracking()
            .Where(gm => gm.UserId == userId)
            .GroupJoin(
                _dbSet.AsNoTracking().Where(m => m.GroupId != null),
                gm => gm.GroupId,
                m => m.GroupId,
                (gm, messages) => new { GroupId = gm.GroupId, Messages = messages }
            )
            .Select(g => new Core.Domain.Models.RecentChatResult
            {
                OtherUserId = null,
                GroupId = g.GroupId,
                LastMessage = g.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault(),
                UnreadCount = 0 // Group unread count not yet implemented
            });

        var dms = await dmsQuery.ToListAsync();
        var groups = await groupsQuery.ToListAsync();

        return dms.Concat(groups).OrderByDescending(x => x.LastMessage?.SentAt ?? DateTime.MinValue);
    }

    public async Task<int> GetTotalCountAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(m => m.SenderId == userId || m.ReceiverId == userId);
    }

    public async Task MarkAsReadAsync(Guid userId, Guid senderId)
    {
        var messages = await _dbSet
            .Where(m => m.ReceiverId == userId && m.SenderId == senderId && !m.IsRead)
            .ToListAsync();

        if (messages.Any())
        {
            foreach (var message in messages)
            {
                message.MarkAsRead();
            }
        }
    }

    public async Task<int> DeleteByConversationAsync(Guid? userId1, Guid? userId2, Guid? groupId, CancellationToken cancellationToken = default)
    {
        if (groupId.HasValue)
        {
            return await _dbSet.Where(m => m.GroupId == groupId).ExecuteDeleteAsync(cancellationToken);
        }
        if (userId1.HasValue && userId2.HasValue)
        {
            return await _dbSet.Where(m => m.GroupId == null &&
                ((m.SenderId == userId1 && m.ReceiverId == userId2) || (m.SenderId == userId2 && m.ReceiverId == userId1)))
                .ExecuteDeleteAsync(cancellationToken);
        }
        return 0;
    }

    public async Task<int> GetCountBySenderSinceAsync(Guid senderId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(m => m.SenderId == senderId && m.SentAt >= since, cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetByBranchIdAsync(Guid branchId, int limit, DateTime? before, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.SupportSender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Include(m => m.MessageReactions)
                .ThenInclude(r => r.User)
            .Include(m => m.MessageReactions)
                .ThenInclude(r => r.SupportUser)
            .Where(m => m.BranchId == branchId);

        if (before.HasValue)
            query = query.Where(m => m.SentAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return messages.OrderBy(m => m.SentAt);
    }

    public async Task<IEnumerable<Message>> GetByBranchIdAndClientUserIdAsync(Guid branchId, Guid clientUserId, int limit, DateTime? before, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.SupportSender)
             .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .Include(m => m.ReplyToStoryItem)
                .ThenInclude(i => i!.Story)
                    .ThenInclude(s => s!.User)
            .Include(m => m.MessageReactions)
                .ThenInclude(r => r.User)
            .Include(m => m.MessageReactions)
                .ThenInclude(r => r.SupportUser)
            .Where(m => m.BranchId == branchId && (m.SenderId == clientUserId || (m.SupportSenderId != null && m.TargetReceiverUserId == clientUserId)));

        if (before.HasValue)
            query = query.Where(m => m.SentAt < before.Value);

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return messages.OrderBy(m => m.SentAt);
    }
}
