using Core.Domain.Entities;

namespace Core.Application.Interfaces;

/// <summary>
/// JWT token generation for support users. Uses separate config (e.g. Jwt:SupportUser) from main user JWT.
/// </summary>
public interface ISupportUserJwtTokenGenerator
{
    string GenerateToken(SupportUser supportUser);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
