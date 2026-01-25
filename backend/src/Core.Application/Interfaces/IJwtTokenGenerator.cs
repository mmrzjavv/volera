using Core.Domain.Entities;

namespace Core.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}