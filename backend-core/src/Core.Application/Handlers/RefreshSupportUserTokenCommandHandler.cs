using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class RefreshSupportUserTokenCommandHandler : IRequestHandler<RefreshSupportUserTokenCommand, SupportUserAuthResultDto?>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly ISupportUserJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshSupportUserTokenCommandHandler(
        ISupportUserRepository supportUserRepository,
        ISupportUserJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork)
    {
        _supportUserRepository = supportUserRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupportUserAuthResultDto?> Handle(RefreshSupportUserTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return null;

        var supportUserIdClaim = principal.FindFirst("supportUserId")?.Value;
        if (string.IsNullOrEmpty(supportUserIdClaim) || !Guid.TryParse(supportUserIdClaim, out var supportUserId))
            return null;

        var supportUser = await _supportUserRepository.GetByIdAsync(supportUserId);
        if (supportUser == null || !supportUser.IsActive)
            return null;

        var refreshTokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        if (supportUser.RefreshToken != refreshTokenHash ||
            !supportUser.RefreshTokenExpiryTime.HasValue ||
            supportUser.RefreshTokenExpiryTime.Value < DateTime.UtcNow)
            return null;

        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshExpiry = DateTime.UtcNow.AddDays(7);
        supportUser.UpdateRefreshToken(newRefreshToken, newRefreshExpiry);
        _supportUserRepository.Update(supportUser);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(supportUser);
        return new SupportUserAuthResultDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            SupportUser = new SupportUserDto
            {
                Id = supportUser.Id,
                CompanyId = supportUser.CompanyId,
                Username = supportUser.Username,
                FirstName = supportUser.FirstName,
                LastName = supportUser.LastName,
                Email = supportUser.Email,
                PhoneNumber = supportUser.PhoneNumber,
                Role = supportUser.Role.ToRoleName()
            }
        };
    }
}
