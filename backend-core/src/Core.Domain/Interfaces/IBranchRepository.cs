using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IBranchRepository : IRepository<Branch>
{
    Task<IEnumerable<Branch>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
