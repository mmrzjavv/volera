using Core.Application.Interfaces;

namespace WebAPI.Services;

/// <summary>
/// Signs media URLs against the browser-facing host (via nginx), not localhost:9000.
/// That way phones on the LAN receive reachable same-origin MinIO proxy links.
/// </summary>
public class RequestHostPublicStorageEndpointProvider : IPublicStorageEndpointProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public RequestHostPublicStorageEndpointProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

        public string? GetPublicBaseUrl()
    {
        var mode = _configuration["Storage:PublicEndpointMode"];
        var configured = _configuration["Storage:PublicEndpointUrl"];
        var useRequestHost =
            string.Equals(mode, "RequestHost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configured, "auto", StringComparison.OrdinalIgnoreCase)
            || IsLoopbackPublicEndpoint(configured);

        var request = _httpContextAccessor.HttpContext?.Request;
        if (useRequestHost && request is not null)
        {
            // Prefer forwarded host from reverse proxy (includes port when using $http_host).
            var host = FirstHeader(request, "X-Forwarded-Host") ?? request.Host.Value;
            if (!string.IsNullOrWhiteSpace(host))
            {
                // If proxy forwarded hostname without port, keep the request's non-default port.
                if (!host.Contains(':') && request.Host.Port is int port
                    && port is not (80 or 443))
                {
                    host = $"{host}:{port}";
                }

                var proto = FirstHeader(request, "X-Forwarded-Proto")
                    ?? request.Scheme
                    ?? "http";
                return $"{proto}://{host}".TrimEnd('/');
            }
        }

        // Background jobs (outbox): fall back to configured non-loopback public URL when set.
        if (!string.IsNullOrWhiteSpace(configured)
            && !string.Equals(configured, "auto", StringComparison.OrdinalIgnoreCase)
            && !IsLoopbackPublicEndpoint(configured))
        {
            return configured.TrimEnd('/');
        }

        return null;
    }

    private static bool IsLoopbackPublicEndpoint(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return true;
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri))
            return false;
        return uri.Host is "localhost" or "127.0.0.1" or "::1";
    }

    private static string? FirstHeader(HttpRequest request, string name)
    {
        if (!request.Headers.TryGetValue(name, out var values))
            return null;
        var raw = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        // X-Forwarded-Host may be a comma-separated list
        return raw.Split(',')[0].Trim();
    }
}
