using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICompanyClientRepository : IRepository<CompanyClient>
{
    Task<CompanyClient?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<CompanyClient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, CompanyClient>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
