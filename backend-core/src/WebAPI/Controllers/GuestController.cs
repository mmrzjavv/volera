using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Core.Application.Commands;
using WebAPI.Extensions;
using WebAPI.Models;

namespace WebAPI.Controllers;

/// <summary>
/// All routes are AllowAnonymous; identity is via X-Guest-Token, not JWT.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/guest")]
public class GuestController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GuestController> _logger;

    public const string GuestTokenHeaderName = "X-Guest-Token";

    public GuestController(IMediator mediator, ILogger<GuestController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Create a guest session. At least one of email or mobile is required. Rate limited per IP.</summary>
    [HttpPost("session")]
    [EnableRateLimiting("GuestCreateSession")]
    public async Task<IActionResult> CreateSession([FromBody] CreateGuestSessionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Guest session creation requested.");
        var command = new CreateGuestSessionCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = request.Mobile
        };
        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation("Guest session created. GuestId: {GuestId}", result.GuestId);
        return new ObjectResult(ApiResponse<object>.Ok(new
        {
            guestToken = result.GuestToken,
            guestId = result.GuestId,
            expiresAt = result.ExpiresAt
        })) { StatusCode = 201 };
    }

    /// <summary>Send a message as a guest. Requires X-Guest-Token header.</summary>
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendGuestMessageRequest request, CancellationToken cancellationToken)
    {
        var token = Request.Headers[GuestTokenHeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            return this.ApiUnauthorized("Guest token is required. Send X-Guest-Token header.");

        var command = new SendGuestMessageCommand
        {
            GuestToken = token,
            Content = request.Content ?? "",
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType
        };
        var messageId = await _mediator.Send(command, cancellationToken);
        return this.Success(new { messageId });
    }
}

public class CreateGuestSessionRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

public class SendGuestMessageRequest
{
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
}
