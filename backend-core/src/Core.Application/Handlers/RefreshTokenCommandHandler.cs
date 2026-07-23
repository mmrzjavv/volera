using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;
using AutoMapper;

namespace Core.Application.Handlers;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISessionService _sessionService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISessionService sessionService)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _sessionService = sessionService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _refreshTokenHasher.Hash(request.RefreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);

        if (session == null || !session.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (session.RefreshTokenExpiryAt == null || session.RefreshTokenExpiryAt.Value <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var user = await _userRepository.GetByIdAsync(session.UserId);
        if (user == null || !user.CanUseSystem)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        session.Touch();
        if (!string.IsNullOrWhiteSpace(request.AppVersion))
            session.UpdateAppVersion(request.AppVersion.Trim());

        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshToken);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);
        session.UpdateRefreshToken(newRefreshTokenHash, refreshExpiry);

        _sessionRepository.Update(session);
        await _unitOfWork.SaveChangesAsync();

        await _sessionService.InvalidateSessionCacheAsync(session.Id, cancellationToken);
        await _sessionService.SaveSessionToCacheAsync(session, cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user, session.Id);
        var sessions = await _sessionService.GetActiveSessionsForUserAsync(user.Id, excludeSessionId: null, cancellationToken);

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = _mapper.Map<UserDto>(user),
            Sessions = sessions
        };
    }
}
