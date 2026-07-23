using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SupportUserLoginByUsernameCommandHandler : IRequestHandler<SupportUserLoginByUsernameCommand, SupportUserAuthResultDto?>
{
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISupportUserJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public SupportUserLoginByUsernameCommandHandler(
        ISupportUserRepository supportUserRepository,
        IPasswordHasher passwordHasher,
        ISupportUserJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _supportUserRepository = supportUserRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<SupportUserAuthResultDto?> Handle(SupportUserLoginByUsernameCommand request, CancellationToken cancellationToken)
    {
        var supportUser = await _supportUserRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (supportUser == null || !_passwordHasher.VerifyPassword(request.Password, supportUser.PasswordHash))
            return null;

        if (!supportUser.IsActive)
            return null;

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(7);
        supportUser.UpdateRefreshToken(refreshToken, refreshExpiry);
        _supportUserRepository.Update(supportUser);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(supportUser);
        return new SupportUserAuthResultDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            SupportUser = MapToDto(supportUser)
        };
    }

    private static SupportUserDto MapToDto(SupportUser u)
    {
        return new SupportUserDto
        {
            Id = u.Id,
            CompanyId = u.CompanyId,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToRoleName()
        };
    }
}
