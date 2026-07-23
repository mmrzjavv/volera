using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IMessageReactionRepository : IRepository<MessageReaction>
{
    Task<MessageReaction?> GetByMessageAndUserAsync(Guid messageId, Guid userId);
    Task<MessageReaction?> GetByMessageAndSupportUserAsync(Guid messageId, Guid supportUserId);
    Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(IEnumerable<Guid> messageIds);
}

