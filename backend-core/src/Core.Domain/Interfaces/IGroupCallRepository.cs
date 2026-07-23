using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IGroupCallRepository : IRepository<GroupCall>
{
    Task<GroupCall?> GetByIdWithParticipantsAsync(Guid id);
    Task<GroupCall?> GetActiveByGroupIdAsync(Guid groupId);
    Task<(IEnumerable<GroupCall> Items, int TotalCount)> GetHistoryByGroupIdAsync(Guid groupId, int page, int pageSize);
}

