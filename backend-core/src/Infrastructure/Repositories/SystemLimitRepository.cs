using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SystemLimitRepository : Repository<SystemLimit>, ISystemLimitRepository
{
    public SystemLimitRepository(ApplicationDbContext context) : base(context) { }

    public async Task<SystemLimit?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.SystemLimits.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<IEnumerable<SystemLimit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemLimits.AsNoTracking().ToListAsync(cancellationToken);
    }
}
