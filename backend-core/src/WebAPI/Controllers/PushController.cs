using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Application.Interfaces;
using WebAPI.Services;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class PushController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;

    public PushController(IPushNotificationService pushNotificationService)
    {
        _pushNotificationService = pushNotificationService;
    }

    [HttpGet("vapid-public-key")]
    public async Task<IActionResult> GetVapidPublicKey()
    {
        var publicKey = await _pushNotificationService.GetVapidPublicKeyAsync();
        return this.Success(new { publicKey });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());

        await _pushNotificationService.AddSubscriptionAsync(
            userId,
            dto.Endpoint,
            dto.Keys.P256dh,
            dto.Keys.Auth
        );

        return this.Success(new { message = "Subscription saved successfully" });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());

        await _pushNotificationService.RemoveAllSubscriptionsAsync(userId);

        return this.Success(new { message = "Unsubscribed successfully" });
    }
}

public class PushSubscriptionDto
{
    public string Endpoint { get; set; } = string.Empty;
    public PushKeysDto Keys { get; set; } = new();
}

public class PushKeysDto
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
