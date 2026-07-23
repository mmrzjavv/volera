using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ISystemLimitRepository : IRepository<SystemLimit>
{
    Task<SystemLimit?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemLimit>> GetAllAsync(CancellationToken cancellationToken = default);
}
