using System.Security.Cryptography;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Infrastructure.Security;

public class CompanyTokenService : ICompanyTokenService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IRefreshTokenHasher _hasher;

    public CompanyTokenService(ICompanyRepository companyRepository, IRefreshTokenHasher hasher)
    {
        _companyRepository = companyRepository;
        _hasher = hasher;
    }

    public string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public async Task<Company?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = _hasher.Hash(token);
        var company = await _companyRepository.GetByRegistrationTokenHashAsync(hash, cancellationToken);
        if (company == null || !company.TokenExpiresAt.HasValue || company.TokenExpiresAt.Value < DateTime.UtcNow)
            return null;
        return company;
    }
}
