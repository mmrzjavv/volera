using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

/// <summary>
/// Creates both User (Guest role) and Guest in one transaction to keep Message pipeline unchanged.
/// </summary>
public class CreateGuestSessionCommandHandler : IRequestHandler<CreateGuestSessionCommand, CreateGuestSessionResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGuestTokenService _guestTokenService;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;

    private const string GuestPasswordPlaceholder = "guest-no-login-placeholder";

    public CreateGuestSessionCommandHandler(
        IUserRepository userRepository,
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        IGuestTokenService guestTokenService,
        IRefreshTokenHasher tokenHasher,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
        _guestTokenService = guestTokenService;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateGuestSessionResult> Handle(CreateGuestSessionCommand request, CancellationToken cancellationToken)
    {
        // Username max 20, PhoneNumber max 15 in DB - keep both unique and within limits
        var guestPrefix = Guid.NewGuid().ToString("N")[..10];
        var username = $"g_{guestPrefix}";
        var phoneNumber = $"g{guestPrefix}";

        var passwordHash = _passwordHasher.HashPassword(GuestPasswordPlaceholder);
        var user = new User(
            request.FirstName ?? "",
            request.LastName ?? "",
            username,
            phoneNumber,
            passwordHash,
            UserRole.Guest);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _guestTokenService.GenerateSecureToken();
        var tokenHash = _tokenHasher.Hash(token);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var guest = new Guest(
            user.Id,
            tokenHash,
            expiresAt,
            request.FirstName?.Trim().NullIfEmpty(),
            request.LastName?.Trim().NullIfEmpty(),
            request.Email?.Trim().NullIfEmpty(),
            request.Mobile?.Trim().NullIfEmpty());

        await _guestRepository.AddAsync(guest);
        await _unitOfWork.SaveChangesAsync();

        return new CreateGuestSessionResult
        {
            GuestToken = token,
            GuestId = guest.Id,
            ExpiresAt = expiresAt
        };
    }
}

internal static class GuestSessionStringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
