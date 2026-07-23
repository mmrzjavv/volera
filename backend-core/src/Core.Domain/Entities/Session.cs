using Shared;

namespace Core.Domain.Entities;

public class Session : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string DeviceType { get; private set; } = string.Empty;
    public string Browser { get; private set; } = string.Empty;
    public string OS { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTime LoginAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public string AppVersion { get; private set; } = string.Empty;
    public DateTime? RevokedAt { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiryAt { get; private set; }

    private Session() { } // EF Core

    public Session(
        Guid userId,
        string deviceType,
        string browser,
        string os,
        string location,
        string appVersion,
        string? refreshTokenHash,
        DateTime? refreshTokenExpiryAt)
    {
        UserId = userId;
        DeviceType = deviceType ?? string.Empty;
        Browser = browser ?? string.Empty;
        OS = os ?? string.Empty;
        Location = location ?? string.Empty;
        AppVersion = appVersion ?? string.Empty;
        LoginAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiryAt = refreshTokenExpiryAt;
    }

    public void Touch()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string? refreshTokenHash, DateTime? expiryAt)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiryAt = expiryAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAppVersion(string appVersion)
    {
        if (!string.IsNullOrWhiteSpace(appVersion))
        {
            AppVersion = appVersion.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Revoke()
    {
        if (RevokedAt == null)
        {
            RevokedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsActive => RevokedAt == null;
}
