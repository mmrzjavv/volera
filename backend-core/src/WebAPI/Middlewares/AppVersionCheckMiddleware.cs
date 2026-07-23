using Core.Application.Interfaces;
using WebAPI.Services;

namespace WebAPI.Middlewares;

/// <summary>
/// After authentication, checks if the session's app version is older than the current required version.
/// If outdated, sets response headers: X-App-Update-Required: true, X-Current-App-Version: &lt;version&gt;.
/// Frontend should react to these headers only (no client-side version logic).
/// </summary>
public class AppVersionCheckMiddleware
{
    public const string HeaderUpdateRequired = "X-App-Update-Required";
    public const string HeaderCurrentVersion = "X-Current-App-Version";

    private readonly RequestDelegate _next;

    public AppVersionCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService, IAppVersionService appVersionService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sessionIdClaim = context.User.FindFirst("sessionId")?.Value;
            if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                var session = await sessionService.GetSessionAsync(sessionId, context.RequestAborted);
                if (session == null)
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                var currentVersion = await appVersionService.GetVersionAsync(context.RequestAborted);
                if (!string.IsNullOrWhiteSpace(currentVersion) && IsSessionVersionOutdated(session.AppVersion, currentVersion))
                {
                    context.Response.OnStarting(() =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.Response.Headers[HeaderUpdateRequired] = "true";
                            context.Response.Headers[HeaderCurrentVersion] = currentVersion;
                        }
                        return Task.CompletedTask;
                    });
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Returns true when sessionVersion is older than currentVersion (or session version is unknown/empty).
    /// Uses simple semantic-style comparison (e.g. 1.2.3 vs 1.2.4); falls back to string compare.
    /// </summary>
    private static bool IsSessionVersionOutdated(string sessionVersion, string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(sessionVersion))
            return true;

        var sessionParts = sessionVersion.Trim().Split('.');
        var currentParts = currentVersion.Trim().Split('.');

        for (var i = 0; i < Math.Max(sessionParts.Length, currentParts.Length); i++)
        {
            var s = i < sessionParts.Length ? sessionParts[i] : "0";
            var c = i < currentParts.Length ? currentParts[i] : "0";

            if (!int.TryParse(s, out var si)) si = 0;
            if (!int.TryParse(c, out var ci)) ci = 0;

            if (si < ci) return true;
            if (si > ci) return false;
        }

        return false;
    }
}
