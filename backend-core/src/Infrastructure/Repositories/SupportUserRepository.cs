using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SupportUserRepository : Repository<SupportUser>, ISupportUserRepository
{
    public SupportUserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<SupportUser?> GetByCompanyIdAndUsernameAsync(Guid companyId, string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        return await _context.SupportUsers
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Username == username.Trim(), cancellationToken);
    }

    public async Task<SupportUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        return await _context.SupportUsers
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Username == username.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<SupportUser>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportUsers
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.Username)
            .ToListAsync(cancellationToken);
    }
}
