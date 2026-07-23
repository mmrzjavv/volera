using System.Text;
using System.Text.Json;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Middlewares;

/// <summary>
/// Middleware to automatically encrypt/decrypt request and response bodies
/// </summary>
public class EncryptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EncryptionMiddleware> _logger;

    public EncryptionMiddleware(RequestDelegate next, ILogger<EncryptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IEncryptionService encryptionService,
        ISessionKeyManager sessionKeyManager)
    {
        try
        {
            // Skip encryption for key exchange endpoint, auth endpoints (login/register), and static files
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // Early exit for endpoints that should never be encrypted
            if (path.Contains("/api/auth/keyexchange") || 
                path.Contains("/api/auth/login") ||
                path.Contains("/api/auth/register") ||
                path.Contains("/swagger") ||
                path.StartsWith("/callhub") ||
                path.StartsWith("/chathub") ||
                !path.StartsWith("/api"))
            {
                await _next(context);
                return;
            }

            // For all other endpoints, check if encryption is needed
            // This will only apply to authenticated users with session keys
            // Note: Authentication happens AFTER this middleware, so context.User may be null

            // Get user ID from JWT token
            var userIdClaim = context.User?.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                // Not authenticated, skip encryption
                await _next(context);
                return;
            }

            // Check if user has a session key
            byte[]? sessionKey = null;
            try
            {
                sessionKey = sessionKeyManager.GetSessionKey(userId);
                if (sessionKey != null)
                {
                    _logger.LogInformation("[BACKEND] Retrieved session key for user {UserId}. Key (hex): {KeyHex}", 
                        userId, Convert.ToHexString(sessionKey));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting session key for user {UserId}", userId);
            }

            if (sessionKey == null)
            {
                // No session key, skip encryption (will be established via key exchange)
                _logger.LogInformation("[BACKEND] No session key found for user {UserId}, skipping encryption", userId);
                await _next(context);
                return;
            }

        // Decrypt request body if present
        if (context.Request.ContentLength > 0 && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            // Enable buffering so we can read the stream
            context.Request.EnableBuffering();
            
            // Read the entire request body into memory
            context.Request.Body.Position = 0;
            string encryptedBody;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                encryptedBody = await reader.ReadToEndAsync();
            }

            if (!string.IsNullOrEmpty(encryptedBody))
            {
                try
                {
                    // Try to decrypt the body
                    var decryptedBody = encryptionService.Decrypt(encryptedBody, sessionKey);
                    var decryptedBytes = Encoding.UTF8.GetBytes(decryptedBody);
                    
                    // Create a new memory stream with decrypted content
                    var decryptedStream = new MemoryStream(decryptedBytes);
                    
                    // Replace the request body with decrypted content
                    context.Request.Body = decryptedStream;
                    context.Request.ContentLength = decryptedBytes.Length;
                    context.Request.ContentType = "application/json";
                    
                    _logger.LogDebug("Successfully decrypted request body for user {UserId}. Original: {OriginalLength} bytes, Decrypted: {DecryptedLength} bytes", 
                        userId, encryptedBody.Length, decryptedBody.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt request body for user {UserId}. Assuming unencrypted. Error: {Error}", userId, ex.Message);
                    // If decryption fails, assume it's not encrypted (backward compatibility)
                    // Reset the stream to the beginning with original content
                    context.Request.Body.Position = 0;
                    // Note: The body is already at position 0 from EnableBuffering, but we need to reset it
                    // Since we read it, we need to create a new stream with the original content
                    var originalBytes = Encoding.UTF8.GetBytes(encryptedBody);
                    context.Request.Body = new MemoryStream(originalBytes);
                    context.Request.ContentLength = originalBytes.Length;
                }
            }
            else
            {
                // Empty body, create empty stream
                context.Request.Body = new MemoryStream();
                context.Request.ContentLength = 0;
            }
        }

        // Capture response body
        var originalResponseBody = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request pipeline");
                // Restore original response body and rethrow
                context.Response.Body = originalResponseBody;
                throw;
            }

            // Encrypt response body only for successful JSON responses
            // Check ContentType from headers first, then from response
            var contentType = context.Response.ContentType ?? context.Response.Headers["Content-Type"].ToString();
            if (contentType.Contains("application/json") &&
                context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                if (!string.IsNullOrEmpty(responseText))
                {
                    try
                    {
                        _logger.LogInformation("[BACKEND] Encrypting response for user {UserId}. Response length: {Length}, Session key (hex): {KeyHex}", 
                            userId, responseText.Length, Convert.ToHexString(sessionKey));
                        var encryptedResponse = encryptionService.Encrypt(responseText, sessionKey);
                        var encryptedBytes = Encoding.UTF8.GetBytes(encryptedResponse);
                        
                        context.Response.Body = new MemoryStream(encryptedBytes);
                        context.Response.ContentLength = encryptedBytes.Length;
                        context.Response.ContentType = "application/json";
                        
                        // Copy encrypted response to original stream
                        context.Response.Body.Seek(0, SeekOrigin.Begin);
                        await context.Response.Body.CopyToAsync(originalResponseBody);
                        context.Response.Body = originalResponseBody;
                        _logger.LogInformation("[BACKEND] Response encrypted successfully for user {UserId}. Encrypted length: {Length}", 
                            userId, encryptedResponse.Length);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to encrypt response body for user {UserId}. Error: {Error}", userId, ex.Message);
                        // Fallback to original response (unencrypted)
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalResponseBody);
                        context.Response.Body = originalResponseBody;
                        return;
                    }
                }
            }

            // Copy unencrypted response to original stream (for errors or non-JSON)
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
        }
        catch (Exception ex)
        {
            // If anything goes wrong in the middleware, log and continue without encryption
            _logger.LogError(ex, "Unexpected error in encryption middleware for path {Path}", context.Request.Path);
            await _next(context);
        }
    }
}
