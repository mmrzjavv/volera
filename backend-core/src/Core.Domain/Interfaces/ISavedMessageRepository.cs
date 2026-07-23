using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ISavedMessageRepository : IRepository<SavedMessage>
{
    Task<SavedMessage?> GetByUserAndMessageIdAsync(Guid userId, Guid messageId);
    Task<IEnumerable<SavedMessage>> GetByUserIdAsync(Guid userId, int page, int pageSize);
    Task<int> GetCountByUserIdAsync(Guid userId);
    Task<IEnumerable<Guid>> GetSavedMessageIdsAsync(Guid userId, IEnumerable<Guid> messageIds);
}
