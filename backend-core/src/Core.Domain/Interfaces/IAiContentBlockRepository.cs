using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IAiContentBlockRepository : IRepository<AiContentBlock>
{
    Task<IEnumerable<AiContentBlock>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<AiContentBlock?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}
