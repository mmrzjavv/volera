using Core.Domain.Entities;

namespace Core.Application.Interfaces;

/// <summary>
/// Generates and validates company widget and client session tokens.
/// </summary>
public interface ICompanyWidgetTokenService
{
    string GenerateSecureToken();
    Task<CompanyClient?> ValidateCompanyClientTokenAsync(string token, CancellationToken cancellationToken = default);
}
