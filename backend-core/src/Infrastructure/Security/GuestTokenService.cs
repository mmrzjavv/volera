using System.Security.Cryptography;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Infrastructure.Security;

public class GuestTokenService : IGuestTokenService
{
    private readonly IGuestRepository _guestRepository;
    private readonly IRefreshTokenHasher _hasher;

    public GuestTokenService(IGuestRepository guestRepository, IRefreshTokenHasher hasher)
    {
        _guestRepository = guestRepository;
        _hasher = hasher;
    }

    public string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public async Task<Guest?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = _hasher.Hash(token);
        var guest = await _guestRepository.GetByTokenHashAsync(hash, cancellationToken);
        if (guest == null || guest.TokenExpiresAt < DateTime.UtcNow)
            return null;
        return guest;
    }
}
