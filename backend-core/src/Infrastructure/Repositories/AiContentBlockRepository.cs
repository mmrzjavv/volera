using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AiContentBlockRepository : Repository<AiContentBlock>, IAiContentBlockRepository
{
    public AiContentBlockRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<AiContentBlock>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.AiContentBlocks
            .AsNoTracking()
            .Where(b => b.BranchId == branchId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiContentBlock?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.AiContentBlocks
            .FirstOrDefaultAsync(b => b.JobId == jobId, cancellationToken);
    }
}
