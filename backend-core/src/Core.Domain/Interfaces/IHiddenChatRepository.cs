namespace Core.Domain.Interfaces;

public interface IHiddenChatRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.HiddenChat hiddenChat, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetHiddenUserIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
