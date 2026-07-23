using Microsoft.AspNetCore.Http;

namespace WebAPI.Services;

public class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public RequestContextInfo GetRequestContext()
    {
        var context = _httpContextAccessor.HttpContext;
        var userAgent = context?.Request.Headers.UserAgent.FirstOrDefault() ?? string.Empty;
        var appVersion = context?.Request.Headers["X-App-Version"].FirstOrDefault();
        var (deviceType, browser, os) = ParseUserAgent(userAgent);
        var location = context?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

        return new RequestContextInfo(deviceType, browser, os, location, appVersion);
    }

    private static (string DeviceType, string Browser, string OS) ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return ("Unknown", "Unknown", "Unknown");

        var ua = userAgent.AsSpan();
        var isMobile = ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
                       || ua.Contains("Android", StringComparison.OrdinalIgnoreCase)
                       || ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
                       || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase);

        var deviceType = isMobile ? "Mobile" : "Desktop";
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) deviceType = "Tablet";

        string browser = "Unknown";
        if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) browser = "Edge";
        else if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Edg", StringComparison.OrdinalIgnoreCase)) browser = "Chrome";
        else if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) browser = "Firefox";
        else if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Chrome", StringComparison.OrdinalIgnoreCase)) browser = "Safari";

        string os = "Unknown";
        if (ua.Contains("Windows ", StringComparison.OrdinalIgnoreCase)) os = "Windows";
        else if (ua.Contains("Mac OS", StringComparison.OrdinalIgnoreCase)) os = "macOS";
        else if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) os = "Android";
        else if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) os = "iOS";
        else if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) os = "Linux";

        return (deviceType, browser, os);
    }
}
