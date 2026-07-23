using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

/// <summary>Resolves JWT signing keys. Refuses hardcoded/default secrets.</summary>
public static class JwtConfiguration
{
    public const int MinimumKeyLength = 32;

    public static string RequireSigningKey(IConfiguration configuration, string primaryKey, string? fallbackKey = null)
    {
        var key = configuration[primaryKey];
        if (string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(fallbackKey))
            key = configuration[fallbackKey];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                $"Missing required configuration '{primaryKey}'. Set a strong secret via environment or user-secrets (min {MinimumKeyLength} characters).");
        }

        if (key.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Configuration '{primaryKey}' must be at least {MinimumKeyLength} characters.");
        }

        // Reject well-known insecure placeholders from older templates.
        var normalized = key.Trim();
        if (normalized.Contains("YourSuperSecretKeyHere", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("SupportUserSecretKeyAtLeast32", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("changeme", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration '{primaryKey}' uses an insecure placeholder. Generate a unique secret and set it via environment variables.");
        }

        return key;
    }
}
