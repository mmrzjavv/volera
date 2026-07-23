using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ICompanyRepository : IRepository<Company>
{
    Task<Company?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task<Company?> GetByRegistrationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
