using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Core.Domain.Entities;
using Core.Application.Interfaces;

namespace Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _key = "mysecretkey1234567890123456789012345678901234567890";
        _issuer = "VoiceCallApp";
        _audience = "VoiceCallApp";
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
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
}