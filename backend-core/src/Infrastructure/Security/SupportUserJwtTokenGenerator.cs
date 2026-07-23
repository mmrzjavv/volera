using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Core.Domain.Entities;
using Core.Application.Interfaces;

namespace Infrastructure.Security;

public class SupportUserJwtTokenGenerator : ISupportUserJwtTokenGenerator
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public SupportUserJwtTokenGenerator(IConfiguration configuration)
    {
        _key = JwtConfiguration.RequireSigningKey(configuration, "Jwt:SupportUser:Key", "Jwt:Key");
        _issuer = configuration["Jwt:SupportUser:Issuer"] ?? configuration["Jwt:Issuer"] ?? "Volera-Support";
        _audience = configuration["Jwt:SupportUser:Audience"] ?? configuration["Jwt:Audience"] ?? "Volera-Support";
    }

    public string GenerateToken(SupportUser supportUser)
    {
        var claims = new List<Claim>
        {
            new Claim("supportUserId", supportUser.Id.ToString()),
            new Claim("companyId", supportUser.CompanyId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, supportUser.Id.ToString()),
            new Claim(ClaimTypes.Role, supportUser.Role.ToRoleName()),
            new Claim(JwtRegisteredClaimNames.UniqueName, supportUser.Username),
            new Claim(ClaimTypes.Name, supportUser.Username),
            new Claim("firstName", supportUser.FirstName),
            new Claim("lastName", supportUser.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                ValidateLifetime = false,
                ValidIssuer = _issuer,
                ValidAudience = _audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
