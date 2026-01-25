using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var token = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        // In a real app, store refresh token securely

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }
}