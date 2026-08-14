using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Logging;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/support")]
public class SupportAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SupportAuthController> _logger;

    public SupportAuthController(IMediator mediator, ILogger<SupportAuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SupportLoginRequest request, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (request.CompanyId != null && request.CompanyId != Guid.Empty)
        {
            var command = new SupportUserLoginCommand
            {
                Username = request.Username,
                Password = request.Password,
                CompanyId = request.CompanyId.Value
            };
            var result = await _mediator.Send(command, cancellationToken);
            if (result == null)
            {
                AppLog.Warning(_logger, AppLogEvents.SupportLoginFailed,
                    "Username: {Username} | CompanyId: {CompanyId} | IP: {ClientIp} | Reason: InvalidCredentials | Result: Failure",
                    request.Username, request.CompanyId, clientIp);
                return this.ApiUnauthorized("Invalid username or password.");
            }

            AppLog.Info(_logger, AppLogEvents.SupportLoginSucceeded,
                "SupportUserId: {SupportUserId} | Username: {Username} | CompanyId: {CompanyId} | IP: {ClientIp} | Method: Password | Result: Success",
                result.SupportUser?.Id, request.Username, request.CompanyId, clientIp);
            return this.Success(result);
        }

        var byUsernameCommand = new SupportUserLoginByUsernameCommand
        {
            Username = request.Username,
            Password = request.Password
        };
        var byUsernameResult = await _mediator.Send(byUsernameCommand, cancellationToken);
        if (byUsernameResult == null)
        {
            AppLog.Warning(_logger, AppLogEvents.SupportLoginFailed,
                "Username: {Username} | IP: {ClientIp} | Reason: InvalidCredentials | Result: Failure",
                request.Username, clientIp);
            return this.ApiUnauthorized("Invalid username or password.");
        }

        AppLog.Info(_logger, AppLogEvents.SupportLoginSucceeded,
            "SupportUserId: {SupportUserId} | Username: {Username} | CompanyId: {CompanyId} | IP: {ClientIp} | Method: Password | Result: Success",
            byUsernameResult.SupportUser?.Id, request.Username, byUsernameResult.SupportUser?.CompanyId, clientIp);
        return this.Success(byUsernameResult);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] SupportRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshSupportUserTokenCommand
        {
            AccessToken = request.AccessToken,
            RefreshToken = request.RefreshToken
        };
        var result = await _mediator.Send(command, cancellationToken);
        if (result == null)
        {
            AppLog.Warning(_logger, AppLogEvents.TokenRefreshFailed,
                "IP: {ClientIp} | Reason: InvalidOrExpiredSupportToken | Result: Failure",
                HttpContext.Connection.RemoteIpAddress?.ToString());
            return this.ApiUnauthorized("Invalid or expired token.");
        }

        AppLog.Info(_logger, AppLogEvents.TokenRefreshed,
            "SupportUserId: {SupportUserId} | Result: Success",
            result.SupportUser?.Id);
        return this.Success(result);
    }
}

public class SupportLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>Optional. If not set, login is by username only (first match).</summary>
    public Guid? CompanyId { get; set; }
}

public class SupportRefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
