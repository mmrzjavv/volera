using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Domain.Interfaces;

public interface ISuggestedPostRepository : IRepository<SuggestedPost>
{
    Task<IReadOnlyList<SuggestedPost>> GetByChannelAsync(Guid channelId, SuggestedPostStatus? status, CancellationToken cancellationToken = default);
    Task<SuggestedPost?> GetByIdWithChannelAsync(Guid id, CancellationToken cancellationToken = default);
}
