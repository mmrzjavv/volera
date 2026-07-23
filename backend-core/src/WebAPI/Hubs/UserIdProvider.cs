using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebAPI.Hubs;

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Try to get userId from NameIdentifier claim (set in JWT validation)
        var userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Fallback to userId claim if NameIdentifier is not available
        if (string.IsNullOrEmpty(userId))
        {
            userId = connection.User?.FindFirst("userId")?.Value;
        }
        
        return userId;
    }
}
