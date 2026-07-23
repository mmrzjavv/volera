using System.Security.Cryptography;
using System.Text;
using Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Infrastructure.Security;

/// <summary>
/// ECDH key exchange service for secure session key establishment
/// Uses ECDH P-256 (secp256r1) for key exchange
/// </summary>
public class EcdhKeyExchangeService : IKeyExchangeService
{
    private const int SessionKeySize = 32; // AES-256 key size
    private readonly ILogger<EcdhKeyExchangeService> _logger;

    public EcdhKeyExchangeService(ILogger<EcdhKeyExchangeService> logger)
    {
        _logger = logger;
    }

    public string GenerateServerKeyPair()
    {
        // Generate ECDH key pair
        using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
        {
            // Export public key
            var publicKeyBytes = ecdh.PublicKey.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(publicKeyBytes);
        }
    }

    public byte[] DeriveSessionKey(string clientPublicKey, string serverPrivateKey)
    {
        if (string.IsNullOrEmpty(clientPublicKey))
            throw new ArgumentException("Client public key is required", nameof(clientPublicKey));

        if (string.IsNullOrEmpty(serverPrivateKey))
            throw new ArgumentException("Server private key is required", nameof(serverPrivateKey));

        var clientPublicKeyBytes = Convert.FromBase64String(clientPublicKey);
        var serverPrivateKeyBytes = Convert.FromBase64String(serverPrivateKey);

        // Import server's private key
        using (var serverEcdh = ECDiffieHellman.Create())
        {
            serverEcdh.ImportPkcs8PrivateKey(serverPrivateKeyBytes, out _);

            // Import client's public key
            using (var clientEcdh = ECDiffieHellman.Create())
            {
                clientEcdh.ImportSubjectPublicKeyInfo(clientPublicKeyBytes, out _);

                // Derive shared secret
                var sharedSecret = serverEcdh.DeriveKeyMaterial(clientEcdh.PublicKey);
                _logger.LogInformation("[BACKEND] Shared secret derived. Length: {Length}, First 8 bytes (hex): {FirstBytes}",
                    sharedSecret.Length, Convert.ToHexString(sharedSecret.Take(8).ToArray()));

                // Derive session key using HKDF (RFC 5869) - use built-in .NET HKDF
                var info = Encoding.UTF8.GetBytes("VoiceCallApp-SessionKey");
                // Use 32-byte zero-filled salt to match frontend behavior
                // Frontend is using 32-byte zero-filled salt based on logs
                var saltBytes = new byte[32];
                Array.Fill<byte>(saltBytes, 0);
                
                _logger.LogInformation("[BACKEND] HKDF parameters - Salt length: {SaltLength}, Salt (hex): {SaltHex}, Info: {Info}",
                    saltBytes.Length, Convert.ToHexString(saltBytes), Encoding.UTF8.GetString(info));
                
                // Use .NET's built-in HKDF which matches Web Crypto API
                var sessionKey = HKDF.DeriveKey(
                    HashAlgorithmName.SHA256,
                    sharedSecret,
                    SessionKeySize,
                    saltBytes,
                    info);
                
                _logger.LogInformation("[BACKEND] Session key derived. Length: {Length}, Key (hex): {KeyHex}",
                    sessionKey.Length, Convert.ToHexString(sessionKey));
                
                return sessionKey;
            }
        }
    }

    public byte[] GenerateSessionKey()
    {
        var key = new byte[SessionKeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

}
