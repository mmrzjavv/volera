using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IStoryRepository : IRepository<Story>
{
    Task<Story?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StoryItem?> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Story>> GetActiveStoriesForUsersAsync(IEnumerable<Guid> userIds, DateTime utcNow, CancellationToken cancellationToken = default);
    Task SoftDeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<StoryView?> GetViewAsync(Guid storyId, Guid viewerUserId, CancellationToken cancellationToken = default);
    Task AddViewAsync(StoryView view, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoryView>> GetViewsForStoryAsync(Guid storyId, CancellationToken cancellationToken = default);
    void RemoveItem(StoryItem item);
}
