using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StoryRepository : Repository<Story>, IStoryRepository
{
    public StoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Story?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Stories
            .Include(s => s.Items)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<StoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return await _context.StoryItems
            .Include(i => i.Story)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
    }

    public async Task<IReadOnlyList<Story>> GetActiveStoriesForUsersAsync(
        IEnumerable<Guid> userIds,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return Array.Empty<Story>();

        return await _context.Stories
            .Include(s => s.Items)
            .Include(s => s.User)
            .Include(s => s.Views)
            .Where(s => ids.Contains(s.UserId) && s.DeletedAt == null && s.ExpiresAt > utcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SoftDeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var expired = await _context.Stories
            .Where(s => s.DeletedAt == null && s.ExpiresAt <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var story in expired)
            story.SoftDelete();
    }

    public async Task<StoryView?> GetViewAsync(Guid storyId, Guid viewerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.StoryViews
            .FirstOrDefaultAsync(v => v.StoryId == storyId && v.ViewerUserId == viewerUserId, cancellationToken);
    }

    public async Task AddViewAsync(StoryView view, CancellationToken cancellationToken = default)
    {
        await _context.StoryViews.AddAsync(view, cancellationToken);
    }

    public async Task<IReadOnlyList<StoryView>> GetViewsForStoryAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        return await _context.StoryViews
            .Include(v => v.ViewerUser)
            .Where(v => v.StoryId == storyId)
            .OrderByDescending(v => v.ViewedAt)
            .ToListAsync(cancellationToken);
    }

    public void RemoveItem(StoryItem item)
    {
        _context.StoryItems.Remove(item);
    }
}
