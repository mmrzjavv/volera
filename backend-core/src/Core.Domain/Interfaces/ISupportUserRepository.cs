using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface ISupportUserRepository : IRepository<SupportUser>
{
    Task<SupportUser?> GetByCompanyIdAndUsernameAsync(Guid companyId, string username, CancellationToken cancellationToken = default);
    /// <summary>Find first support user with the given username (any company).</summary>
    Task<SupportUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupportUser>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
