namespace Core.Domain.Interfaces;

public interface IMessageViewRepository
{
    Task<int> RecordViewsAsync(Guid userId, IReadOnlyList<Guid> messageIds, CancellationToken cancellationToken = default);
    Task<bool> HasViewedAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);
}
