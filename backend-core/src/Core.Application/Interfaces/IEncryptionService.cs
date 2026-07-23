namespace Core.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting data using AES-GCM
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using AES-GCM with a session key
    /// </summary>
    /// <param name="plaintext">Data to encrypt</param>
    /// <param name="sessionKey">Session key (32 bytes for AES-256)</param>
    /// <returns>Encrypted data with IV and tag (Base64 encoded)</returns>
    string Encrypt(string plaintext, byte[] sessionKey);

    /// <summary>
    /// Decrypts data encrypted with AES-GCM
    /// </summary>
    /// <param name="encryptedData">Encrypted data with IV and tag (Base64 encoded)</param>
    /// <param name="sessionKey">Session key (32 bytes for AES-256)</param>
    /// <returns>Decrypted plaintext</returns>
    string Decrypt(string encryptedData, byte[] sessionKey);

    /// <summary>
    /// Encrypts binary data
    /// </summary>
    byte[] EncryptBytes(byte[] plaintext, byte[] sessionKey);

    /// <summary>
    /// Decrypts binary data
    /// </summary>
    byte[] DecryptBytes(byte[] encryptedData, byte[] sessionKey);
}
