using Core.Domain.Entities;

namespace Core.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    /// <summary>Generates an access token with sessionId claim for session-based auth.</summary>
    string GenerateToken(User user, Guid sessionId);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}