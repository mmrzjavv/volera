using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Exceptions;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using AutoMapper;

namespace Core.Application.Handlers;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private const int DefaultMaxSessionsPerUser = 4;

    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISessionService _sessionService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        ISystemLimitRepository systemLimitRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _systemLimitRepository = systemLimitRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _sessionService = sessionService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (user.Role.IsGuest())
            throw new UnauthorizedAccessException("Guest accounts cannot log in.");

        if (!user.CanUseSystem)
            throw new UnauthorizedAccessException("Account is disabled or suspended.");

        var maxSessionsLimit = await _systemLimitRepository.GetByKeyAsync(LimitKeys.MaxSessionsPerUser, cancellationToken);
        var maxSessions = maxSessionsLimit != null ? (int)Math.Max(1, Math.Min(100, maxSessionsLimit.Value)) : DefaultMaxSessionsPerUser;

        var activeCount = await _sessionRepository.CountActiveByUserIdAsync(user.Id, cancellationToken);
        while (activeCount >= maxSessions)
        {
            var oldest = await _sessionRepository.GetOldestActiveSessionByUserIdAsync(user.Id, cancellationToken);
            if (oldest == null) break;
            await _sessionService.RevokeSessionAsync(oldest.Id, cancellationToken);
            activeCount--;
        }

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);

        var session = new Session(
            userId: user.Id,
            deviceType: request.DeviceType ?? "Unknown",
            browser: request.Browser ?? "Unknown",
            os: request.OS ?? "Unknown",
            location: request.Location ?? "Unknown",
            appVersion: request.AppVersion ?? "0.0.0",
            refreshTokenHash: refreshTokenHash,
            refreshTokenExpiryAt: refreshExpiry);

        await _sessionRepository.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        await _sessionService.SaveSessionToCacheAsync(session, cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user, session.Id);
        var sessions = await _sessionService.GetActiveSessionsForUserAsync(user.Id, excludeSessionId: null, cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = _mapper.Map<UserDto>(user),
            Sessions = sessions
        };
    }
}