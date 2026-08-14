using System.Collections.Concurrent;
using System.Security.Cryptography;
using Core.Application.Interfaces;
using System;

namespace Infrastructure.Security;

/// <summary>
/// In-memory session key manager
/// In production, consider using Redis or distributed cache
/// </summary>
public class InMemorySessionKeyManager : ISessionKeyManager
{
    private readonly ConcurrentDictionary<Guid, SessionKeyEntry> _sessionKeys = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

    private class SessionKeyEntry
    {
        public byte[] Key { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }

    public void SetSessionKey(Guid userId, byte[] sessionKey, TimeSpan? expiresIn = null)
    {
        var expiration = expiresIn ?? _defaultExpiration;
        _sessionKeys.AddOrUpdate(
            userId,
            new SessionKeyEntry
            {
                Key = sessionKey,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            },
            (key, existing) => new SessionKeyEntry
            {
                Key = sessionKey,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            });

        CleanupExpiredKeys();
    }

    public byte[]? GetSessionKey(Guid userId)
    {
        if (!_sessionKeys.TryGetValue(userId, out var entry))
            return null;

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _sessionKeys.TryRemove(userId, out _);
            return null;
        }

        return entry.Key;
    }

    public void RemoveSessionKey(Guid userId)
    {
        _sessionKeys.TryRemove(userId, out _);
    }

    public bool HasValidSessionKey(Guid userId)
    {
        if (!_sessionKeys.TryGetValue(userId, out var entry))
            return false;

        if (entry.ExpiresAt < DateTime.UtcNow)
        {
            _sessionKeys.TryRemove(userId, out _);
            return false;
        }

        return true;
    }

    public byte[] RotateSessionKey(Guid userId)
    {
        var newKey = new byte[32];
        RandomNumberGenerator.Fill(newKey);
        SetSessionKey(userId, newKey);
        return newKey;
    }

    private void CleanupExpiredKeys()
    {
        // Cleanup every 100 operations to avoid performance impact
        if (_sessionKeys.Count % 100 == 0)
        {
            var expiredKeys = _sessionKeys
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _sessionKeys.TryRemove(key, out _);
            }
        }
    }
}
