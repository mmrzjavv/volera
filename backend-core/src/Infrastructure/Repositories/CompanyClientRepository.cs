using System.Collections.Generic;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyClientRepository : Repository<CompanyClient>, ICompanyClientRepository
{
    public CompanyClientRepository(ApplicationDbContext context) : base(context) { }

    public async Task<CompanyClient?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tokenHash)) return null;
        return await _context.CompanyClients
            .Include(c => c.Company)
            .Include(c => c.Branch)
            .Include(c => c.CompanyWidget)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.SessionTokenHash == tokenHash && c.TokenExpiresAt > DateTime.UtcNow, cancellationToken);
    }

    public async Task<CompanyClient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyClients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CompanyClient>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, CompanyClient>();
        var list = await _context.CompanyClients
            .Include(c => c.User)
            .Where(c => idList.Contains(c.UserId))
            .ToListAsync(cancellationToken);
        return list.ToDictionary(c => c.UserId);
    }
}
