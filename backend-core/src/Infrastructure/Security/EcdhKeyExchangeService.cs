using System.Security.Cryptography;
using System.Text;
using Core.Application.Interfaces;

namespace Infrastructure.Security;

/// <summary>
/// ECDH key exchange service for secure session key establishment
/// Uses ECDH P-256 (secp256r1) for key exchange
/// </summary>
public class EcdhKeyExchangeService : IKeyExchangeService
{
    private const int SessionKeySize = 32; // AES-256 key size

    public string GenerateServerKeyPair()
    {
        using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
        {
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

        using (var serverEcdh = ECDiffieHellman.Create())
        {
            serverEcdh.ImportPkcs8PrivateKey(serverPrivateKeyBytes, out _);

            using (var clientEcdh = ECDiffieHellman.Create())
            {
                clientEcdh.ImportSubjectPublicKeyInfo(clientPublicKeyBytes, out _);

                var sharedSecret = serverEcdh.DeriveKeyMaterial(clientEcdh.PublicKey);

                var info = Encoding.UTF8.GetBytes("VoiceCallApp-SessionKey");
                var saltBytes = new byte[32];
                Array.Fill<byte>(saltBytes, 0);

                return HKDF.DeriveKey(
                    HashAlgorithmName.SHA256,
                    sharedSecret,
                    SessionKeySize,
                    saltBytes,
                    info);
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
