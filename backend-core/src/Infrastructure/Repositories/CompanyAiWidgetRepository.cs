using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyAiWidgetRepository : Repository<CompanyAiWidget>, ICompanyAiWidgetRepository
{
    public CompanyAiWidgetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CompanyAiWidget?> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyAiWidgets
            .AsNoTracking()
            .Include(w => w.Company)
            .Include(w => w.Branch)
            .FirstOrDefaultAsync(w => w.BranchId == branchId, cancellationToken);
    }

    public async Task<IEnumerable<CompanyAiWidget>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyAiWidgets
            .AsNoTracking()
            .Include(w => w.Branch)
            .Where(w => w.CompanyId == companyId)
            .OrderBy(w => w.TenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyAiWidget?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return null;
        return await _context.CompanyAiWidgets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.TenantId == tenantId.Trim() && w.IsActive, cancellationToken);
    }
}
