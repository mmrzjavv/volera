using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.DTOs;
using WebAPI.DTOs;
using Core.Application.Logging;
using WebAPI.Extensions;
using WebAPI.Options;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CallController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CallController> _logger;
    private readonly WebRtcOptions _webRtcOptions;

    public CallController(IMediator mediator, ILogger<CallController> logger, IOptions<WebRtcOptions> webRtcOptions)
    {
        _mediator = mediator;
        _logger = logger;
        _webRtcOptions = webRtcOptions.Value;
    }

    /// <summary>
    /// ICE servers for WebRTC. Uses configured STUN/TURN, or auto-builds from Coturn
    /// (PublicHost or this request's Host) so calls work across different networks.
    /// </summary>
    [HttpGet("ice-servers")]
    public IActionResult GetIceServers()
    {
        var iceServers = new List<IceServerDto>();

        var stunUrls = _webRtcOptions.StunUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToList();

        var turnUrls = _webRtcOptions.TurnUrls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToList();

        // Auto Coturn: same host the client used for the API (LAN IP or public DNS/IP).
        // Prefer PublicHost, then X-Forwarded-Host (Vite HTTPS proxy), then request Host.
        if (stunUrls.Count == 0 && turnUrls.Count == 0 && _webRtcOptions.CoturnEnabled)
        {
            var forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedHost))
            {
                forwardedHost = forwardedHost.Split(',', 2)[0].Trim();
                var colon = forwardedHost.IndexOf(':');
                if (colon > 0) forwardedHost = forwardedHost[..colon];
            }

            var host = !string.IsNullOrWhiteSpace(_webRtcOptions.PublicHost)
                ? _webRtcOptions.PublicHost.Trim()
                : (!string.IsNullOrWhiteSpace(forwardedHost) ? forwardedHost : Request.Host.Host);

            if (!string.IsNullOrWhiteSpace(host) &&
                !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) &&
                host != "127.0.0.1" &&
                host != "::1")
            {
                var port = _webRtcOptions.Port > 0 ? _webRtcOptions.Port : 3478;
                stunUrls.Add($"stun:{host}:{port}");
                turnUrls.Add($"turn:{host}:{port}");
                turnUrls.Add($"turn:{host}:{port}?transport=tcp");
            }
        }

        if (stunUrls.Count > 0)
        {
            iceServers.Add(new IceServerDto { Urls = stunUrls });
        }

        if (turnUrls.Count > 0)
        {
            iceServers.Add(new IceServerDto
            {
                Urls = turnUrls,
                Username = string.IsNullOrWhiteSpace(_webRtcOptions.TurnUsername) ? null : _webRtcOptions.TurnUsername,
                Credential = string.IsNullOrWhiteSpace(_webRtcOptions.TurnCredential) ? null : _webRtcOptions.TurnCredential
            });
        }

        return this.Success(new IceServersResponse { IceServers = iceServers });
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateCall([FromBody] InitiateCallDto dto)
    {
        var callerId = this.GetCurrentUserId();
        if (callerId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new InitiateCallCommand
        {
            CallerId = callerId.Value,
            ReceiverId = dto.ReceiverId,
            IsVideo = dto.IsVideo
        };

        var callId = await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.CallInitiated,
            "UserId: {UserId} | CallId: {CallId} | ReceiverId: {ReceiverId} | IsVideo: {IsVideo} | Result: Success",
            callerId, callId, dto.ReceiverId, dto.IsVideo);
        return this.Success(new { callId });
    }

    [HttpPost("{callId}/accept")]
    public async Task<IActionResult> AcceptCall(Guid callId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new AcceptCallCommand
        {
            CallId = callId,
            UserId = userId.Value
        };

        await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.CallAccepted,
            "UserId: {UserId} | CallId: {CallId} | Result: Success",
            userId, callId);
        return this.Success();
    }

    [HttpPost("{callId}/reject")]
    public async Task<IActionResult> RejectCall(Guid callId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new RejectCallCommand
        {
            CallId = callId,
            UserId = userId.Value
        };

        await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.CallRejected,
            "UserId: {UserId} | CallId: {CallId} | Result: Success",
            userId, callId);
        return this.Success();
    }

    [HttpPost("{callId}/end")]
    public async Task<IActionResult> EndCall(Guid callId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new EndCallCommand
        {
            CallId = callId,
            UserId = userId.Value
        };

        await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.CallEnded,
            "UserId: {UserId} | CallId: {CallId} | Result: Success",
            userId, callId);
        return this.Success();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetCallHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? term = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null)
        {
            return this.ApiUnauthorized();
        }
        var query = new GetCallsByUserIdQuery
        {
            UserId = userId.Value,
            Page = page,
            PageSize = pageSize,
            Term = term,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SortBy = sortBy,
            SortDescending = sortDescending
        };
        var result = await _mediator.Send(query);
        return this.Success(result);
    }
}