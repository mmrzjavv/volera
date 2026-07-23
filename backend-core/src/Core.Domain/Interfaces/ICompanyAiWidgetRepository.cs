using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICompanyAiWidgetRepository : IRepository<CompanyAiWidget>
{
    Task<CompanyAiWidget?> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompanyAiWidget>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CompanyAiWidget?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default);
}
