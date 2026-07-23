using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IUserLimitOverrideRepository : IRepository<UserLimitOverride>
{
    Task<UserLimitOverride?> GetAsync(Guid userId, string limitKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserLimitOverride>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
