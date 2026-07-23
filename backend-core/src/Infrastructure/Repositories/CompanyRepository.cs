using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyRepository : Repository<Company>, ICompanyRepository
{
    public CompanyRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Company?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber)) return null;
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.MobileNumber == mobileNumber.Trim(), cancellationToken);
    }

    public async Task<Company?> GetByRegistrationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tokenHash)) return null;
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.RegistrationTokenHash == tokenHash, cancellationToken);
    }
}
