using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace WebAPI.Logging;

/// <summary>
/// Pushes correlation / user / request context into Serilog LogContext for the request lifetime.
/// Accepts inbound X-Correlation-ID or creates one; also exposes TraceId from Activity / HttpContext.
/// </summary>
public sealed class RequestLogContextMiddleware
{
    public const string CorrelationHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Headers[CorrelationHeaderName] = correlationId;

        var user = context.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst("userId")?.Value
                     ?? user.FindFirst("supportUserId")?.Value;
        var username = user.FindFirstValue(ClaimTypes.Name)
                       ?? user.FindFirst(ClaimTypes.Name)?.Value
                       ?? user.Identity?.Name;
        var sessionId = user.FindFirst("sessionId")?.Value;
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty("ClientIp", clientIp))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("SessionId", sessionId))
        {
            await _next(context);
        }
    }
}
