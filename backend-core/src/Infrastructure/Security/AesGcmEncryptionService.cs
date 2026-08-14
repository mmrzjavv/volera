using System.Security.Cryptography;
using System.Text;
using Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security;

/// <summary>
/// AES-GCM encryption service implementation
/// Uses AES-256-GCM for authenticated encryption
/// </summary>
public class AesGcmEncryptionService : IEncryptionService
{
    private const int KeySize = 32; // AES-256
    private const int NonceSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits authentication tag
    private readonly ILogger<AesGcmEncryptionService> _logger;

    public AesGcmEncryptionService(ILogger<AesGcmEncryptionService> logger)
    {
        _logger = logger;
    }

    public string Encrypt(string plaintext, byte[] sessionKey)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        if (sessionKey == null || sessionKey.Length != KeySize)
            throw new ArgumentException($"Session key must be {KeySize} bytes", nameof(sessionKey));

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var encryptedBytes = EncryptBytes(plaintextBytes, sessionKey);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string encryptedData, byte[] sessionKey)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return string.Empty;

        if (sessionKey == null || sessionKey.Length != KeySize)
            throw new ArgumentException($"Session key must be {KeySize} bytes", nameof(sessionKey));

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var decryptedBytes = DecryptBytes(encryptedBytes, sessionKey);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DecryptionFailed | Error: {ErrorType} | Result: Failure", ex.GetType().Name);
            throw;
        }
    }

    public byte[] EncryptBytes(byte[] plaintext, byte[] sessionKey)
    {
        if (plaintext == null || plaintext.Length == 0)
            return Array.Empty<byte>();

        if (sessionKey == null || sessionKey.Length != KeySize)
            throw new ArgumentException($"Session key must be {KeySize} bytes", nameof(sessionKey));

        // Generate a random nonce (IV) for each encryption
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        // Encrypt using AES-GCM
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aesGcm = new AesGcm(sessionKey, TagSize))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Format: [nonce (12 bytes)][ciphertext+tag (variable)]
        // Web Crypto API returns ciphertext with tag appended, so we match that format
        // Combine ciphertext and tag: [ciphertext][tag]
        var ciphertextWithTag = new byte[ciphertext.Length + TagSize];
        Buffer.BlockCopy(ciphertext, 0, ciphertextWithTag, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, ciphertextWithTag, ciphertext.Length, TagSize);
        
        // Final format: [nonce][ciphertext+tag]
        var result = new byte[NonceSize + ciphertextWithTag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(ciphertextWithTag, 0, result, NonceSize, ciphertextWithTag.Length);

        return result;
    }

    public byte[] DecryptBytes(byte[] encryptedData, byte[] sessionKey)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            return Array.Empty<byte>();

        if (sessionKey == null || sessionKey.Length != KeySize)
            throw new ArgumentException($"Session key must be {KeySize} bytes", nameof(sessionKey));

        if (encryptedData.Length < NonceSize + TagSize)
            throw new ArgumentException("Encrypted data is too short", nameof(encryptedData));

        // Extract nonce, ciphertext, and tag
        // Format: [nonce (12 bytes)][ciphertext+tag (variable)]
        // The tag is at the end of the ciphertext+tag block
        var nonce = new byte[NonceSize];
        var ciphertextWithTag = new byte[encryptedData.Length - NonceSize];
        
        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, ciphertextWithTag, 0, ciphertextWithTag.Length);
        
        // Extract tag from end and ciphertext from beginning
        var tag = new byte[TagSize];
        var ciphertext = new byte[ciphertextWithTag.Length - TagSize];
        
        Buffer.BlockCopy(ciphertextWithTag, 0, ciphertext, 0, ciphertext.Length);
        Buffer.BlockCopy(ciphertextWithTag, ciphertext.Length, tag, 0, TagSize);

        // Decrypt using AES-GCM
        var plaintext = new byte[ciphertext.Length];

        using (var aesGcm = new AesGcm(sessionKey, TagSize))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return plaintext;
    }
}
