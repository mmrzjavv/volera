using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICompanyWidgetRepository : IRepository<CompanyWidget>
{
    Task<CompanyWidget?> GetByWidgetIdAsync(string widgetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompanyWidget>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CompanyWidget>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default);
}
