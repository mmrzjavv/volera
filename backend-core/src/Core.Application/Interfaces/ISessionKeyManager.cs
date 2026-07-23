namespace Core.Application.Interfaces;

/// <summary>
/// Manages session keys for authenticated users
/// </summary>
public interface ISessionKeyManager
{
    /// <summary>
    /// Stores a session key for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="sessionKey">Session key (32 bytes)</param>
    /// <param name="expiresIn">Expiration time (default: 1 hour)</param>
    void SetSessionKey(Guid userId, byte[] sessionKey, TimeSpan? expiresIn = null);

    /// <summary>
    /// Retrieves a session key for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Session key or null if not found/expired</returns>
    byte[]? GetSessionKey(Guid userId);

    /// <summary>
    /// Removes a session key for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    void RemoveSessionKey(Guid userId);

    /// <summary>
    /// Checks if a session key exists and is valid
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if valid session key exists</returns>
    bool HasValidSessionKey(Guid userId);

    /// <summary>
    /// Rotates the session key for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>New session key</returns>
    byte[] RotateSessionKey(Guid userId);
}
