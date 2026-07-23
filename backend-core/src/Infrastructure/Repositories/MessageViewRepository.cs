using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MessageViewRepository : IMessageViewRepository
{
    private readonly ApplicationDbContext _context;

    public MessageViewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasViewedAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.MessageViews.AnyAsync(v => v.MessageId == messageId && v.UserId == userId, cancellationToken);
    }

    public async Task<int> RecordViewsAsync(Guid userId, IReadOnlyList<Guid> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0)
            return 0;

        var distinctIds = messageIds.Distinct().ToList();
        var alreadyViewed = await _context.MessageViews
            .Where(v => v.UserId == userId && distinctIds.Contains(v.MessageId))
            .Select(v => v.MessageId)
            .ToListAsync(cancellationToken);

        var toRecord = distinctIds.Except(alreadyViewed).ToList();
        if (toRecord.Count == 0)
            return 0;

        var messages = await _context.Messages
            .Where(m => toRecord.Contains(m.Id) && m.GroupId != null)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            _context.MessageViews.Add(new MessageView(message.Id, userId));
            message.IncrementViewCount();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
