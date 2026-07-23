namespace Core.Application.Interfaces;

/// <summary>
/// Resolves the browser-reachable base URL used when signing media download/upload links.
/// Prefer the incoming request host so LAN mobile clients are not given localhost:9000 URLs.
/// </summary>
public interface IPublicStorageEndpointProvider
{
    /// <returns>Absolute base URL without trailing slash, or null to use configured Storage:PublicEndpointUrl.</returns>
    string? GetPublicBaseUrl();
}
