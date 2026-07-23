using System.Security.Cryptography;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Infrastructure.Security;

public class CompanyWidgetTokenService : ICompanyWidgetTokenService
{
    private readonly ICompanyClientRepository _companyClientRepository;
    private readonly IRefreshTokenHasher _hasher;

    public CompanyWidgetTokenService(ICompanyClientRepository companyClientRepository, IRefreshTokenHasher hasher)
    {
        _companyClientRepository = companyClientRepository;
        _hasher = hasher;
    }

    public string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public async Task<CompanyClient?> ValidateCompanyClientTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = _hasher.Hash(token);
        return await _companyClientRepository.GetByTokenHashAsync(hash, cancellationToken);
    }
}
