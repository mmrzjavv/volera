using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BranchRepository : Repository<Branch>, IBranchRepository
{
    public BranchRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Branch>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }
}
