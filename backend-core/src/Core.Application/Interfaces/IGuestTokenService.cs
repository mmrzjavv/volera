using Core.Domain.Entities;

namespace Core.Application.Interfaces;

/// <summary>
/// Generates and validates guest session tokens. Used for REST and SignalR guest auth.
/// </summary>
public interface IGuestTokenService
{
    /// <summary>Returns a cryptographically random token string for the client. Caller hashes and stores in Guest.</summary>
    string GenerateSecureToken();

    /// <summary>Validates the token (hash and lookup), checks expiry; returns Guest or null.</summary>
    Task<Guest?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
