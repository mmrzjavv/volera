using Core.Domain.Entities;

namespace Core.Application.Interfaces;

/// <summary>
/// Generates and validates company session/registration tokens. Used for company auth (OTP TODO).
/// </summary>
public interface ICompanyTokenService
{
    string GenerateSecureToken();
    Task<Company?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
