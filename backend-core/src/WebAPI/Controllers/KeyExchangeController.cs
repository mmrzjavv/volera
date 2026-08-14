using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Interfaces;
using WebAPI.Extensions;
using WebAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace WebAPI.Controllers;

/// <summary>
/// Controller for secure key exchange using ECDH
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class KeyExchangeController : ControllerBase
{
    private readonly IKeyExchangeService _keyExchangeService;
    private readonly ISessionKeyManager _sessionKeyManager;
    private readonly ILogger<KeyExchangeController> _logger;

    // Store server key pairs temporarily (in production, use distributed cache)
    private static readonly Dictionary<string, (string PublicKey, string PrivateKey)> _serverKeys = new();
    private static readonly object _lockObject = new();

    public KeyExchangeController(
        IKeyExchangeService keyExchangeService,
        ISessionKeyManager sessionKeyManager,
        ILogger<KeyExchangeController> logger)
    {
        _keyExchangeService = keyExchangeService;
        _sessionKeyManager = sessionKeyManager;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Client requests server's public key
    /// </summary>
    [HttpPost("keyexchange/init")]
    [AllowAnonymous]
    public IActionResult InitiateKeyExchange([FromBody] KeyExchangeInitRequest request)
    {
        try
        {
            // Generate server key pair
            using (var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                var publicKeyBytes = ecdh.PublicKey.ExportSubjectPublicKeyInfo();
                var privateKeyBytes = ecdh.ExportPkcs8PrivateKey();

                var publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);
                var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);

                // Store server key pair with a temporary ID
                var tempId = Guid.NewGuid().ToString();
                lock (_lockObject)
                {
                    _serverKeys[tempId] = (publicKeyBase64, privateKeyBase64);
                }

                CleanupOldKeys();

                return this.Success(new
                {
                    serverPublicKey = publicKeyBase64,
                    tempId = tempId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KeyExchangeInitFailed | Error: {ErrorType} | Result: Failure", ex.GetType().Name);
            return new ObjectResult(ApiResponse<object?>.Fail("Failed to initiate key exchange")) { StatusCode = 500 };
        }
    }

    /// <summary>
    /// Step 2: Client sends its public key and receives session key
    /// </summary>
    [HttpPost("keyexchange/complete")]
    [Authorize]
    public IActionResult CompleteKeyExchange([FromBody] KeyExchangeCompleteRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());

            // Retrieve server key pair
            (string publicKey, string privateKey) serverKeys;
            lock (_lockObject)
            {
                if (!_serverKeys.TryGetValue(request.TempId, out serverKeys))
                {
                    _logger.LogWarning(
                        "KeyExchangeCompleteFailed | UserId: {UserId} | Reason: TempIdNotFound | Result: Failure",
                        userId);
                    return this.Fail("Invalid or expired temporary key ID");
                }
                _serverKeys.Remove(request.TempId); // Remove after use
            }

            var sessionKey = _keyExchangeService.DeriveSessionKey(request.ClientPublicKey, serverKeys.privateKey);
            _sessionKeyManager.SetSessionKey(userId, sessionKey, TimeSpan.FromHours(1));

            return this.Success(new
            {
                message = "Key exchange completed successfully",
                sessionEstablished = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KeyExchangeCompleteFailed | Error: {ErrorType} | Result: Failure", ex.GetType().Name);
            return new ObjectResult(ApiResponse<object?>.Fail("Failed to complete key exchange")) { StatusCode = 500 };
        }
    }

    private void CleanupOldKeys()
    {
        lock (_lockObject)
        {
            if (_serverKeys.Count > 1000)
            {
                var keysToRemove = _serverKeys.Keys.Take(_serverKeys.Count - 1000).ToList();
                foreach (var key in keysToRemove)
                {
                    _serverKeys.Remove(key);
                }
            }
        }
    }

    public class KeyExchangeInitRequest
    {
        public string? ClientId { get; set; }
    }

    public class KeyExchangeCompleteRequest
    {
        public string ClientPublicKey { get; set; } = null!;
        public string TempId { get; set; } = null!;
    }
}
