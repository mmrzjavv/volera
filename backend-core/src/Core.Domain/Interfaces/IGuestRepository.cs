using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IGuestRepository : IRepository<Guest>
{
    Task<Guest?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Guest?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, Guest>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
