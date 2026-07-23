using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ISupportUserBranchRepository : IRepository<SupportUserBranch>
{
    Task<SupportUserBranch?> GetBySupportUserIdAndBranchIdAsync(Guid supportUserId, Guid branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupportUserBranch>> GetBySupportUserIdAsync(Guid supportUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupportUserBranch>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default);
}
