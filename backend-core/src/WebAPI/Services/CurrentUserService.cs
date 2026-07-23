using System.Security.Claims;
using Core.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Services;

/// <summary>
/// WebAPI implementation of ICurrentUserService using IHttpContextAccessor.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Prefer NameIdentifier (set in JWT events), fall back to raw "userId" claim
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? user.FindFirst("userId")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? SessionId
    {
        get
        {
            var sessionIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("sessionId")?.Value;
            return Guid.TryParse(sessionIdClaim, out var id) ? id : null;
        }
    }
}

