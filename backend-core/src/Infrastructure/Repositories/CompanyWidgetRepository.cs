using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyWidgetRepository : Repository<CompanyWidget>, ICompanyWidgetRepository
{
    public CompanyWidgetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CompanyWidget?> GetByWidgetIdAsync(string widgetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(widgetId)) return null;
        return await _context.CompanyWidgets
            .Include(w => w.Company)
            .Include(w => w.Branch)
            .FirstOrDefaultAsync(w => w.WidgetId == widgetId.Trim() && w.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<CompanyWidget>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyWidgets
            .Include(w => w.Branch)
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.WidgetId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CompanyWidget>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyWidgets
            .Where(w => w.BranchId == branchId)
            .ToListAsync(cancellationToken);
    }
}
