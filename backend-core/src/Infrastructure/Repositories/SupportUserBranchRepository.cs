using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SupportUserBranchRepository : Repository<SupportUserBranch>, ISupportUserBranchRepository
{
    public SupportUserBranchRepository(ApplicationDbContext context) : base(context) { }

    public async Task<SupportUserBranch?> GetBySupportUserIdAndBranchIdAsync(Guid supportUserId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportUserBranches
            .Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.SupportUserId == supportUserId && s.BranchId == branchId, cancellationToken);
    }

    public async Task<IEnumerable<SupportUserBranch>> GetBySupportUserIdAsync(Guid supportUserId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportUserBranches
            .Include(s => s.Branch)
            .Where(s => s.SupportUserId == supportUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SupportUserBranch>> GetByBranchIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        return await _context.SupportUserBranches
            .Include(s => s.SupportUser)
            .Where(s => s.BranchId == branchId)
            .ToListAsync(cancellationToken);
    }
}
