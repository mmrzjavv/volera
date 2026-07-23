namespace Core.Application.Interfaces;

/// <summary>
/// Service for ECDH key exchange and session key management
/// </summary>
public interface IKeyExchangeService
{
    /// <summary>
    /// Generates a server-side ECDH key pair
    /// </summary>
    /// <returns>Server public key (Base64 encoded)</returns>
    string GenerateServerKeyPair();

    /// <summary>
    /// Derives a shared session key from client and server key pairs
    /// </summary>
    /// <param name="clientPublicKey">Client's public key (Base64 encoded)</param>
    /// <param name="serverPrivateKey">Server's private key (Base64 encoded)</param>
    /// <returns>Derived session key (32 bytes for AES-256)</returns>
    byte[] DeriveSessionKey(string clientPublicKey, string serverPrivateKey);

    /// <summary>
    /// Generates a new session key (for key rotation)
    /// </summary>
    /// <returns>New session key (32 bytes)</returns>
    byte[] GenerateSessionKey();
}
