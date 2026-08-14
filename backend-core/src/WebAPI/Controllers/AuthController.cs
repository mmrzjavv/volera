using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Logging;
using WebAPI.Extensions;
using WebAPI.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly IRequestContextService _requestContext;

    public AuthController(IMediator mediator, ILogger<AuthController> logger, IRequestContextService requestContext)
    {
        _mediator = mediator;
        _logger = logger;
        _requestContext = requestContext;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var ctx = _requestContext.GetRequestContext();
        var command = new RegisterUserCommand
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            PhoneNumber = dto.PhoneNumber,
            Password = dto.Password
        };

        try
        {
            var userId = await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.UserRegistered,
                "UserId: {UserId} | Username: {Username} | IP: {ClientIp} | Device: {DeviceType} | Result: Success",
                userId, dto.Username, ctx.Location, ctx.DeviceType);
            return this.Success(new { userId });
        }
        catch (InvalidOperationException ex)
        {
            AppLog.Warning(_logger, AppLogEvents.UserRegistered,
                "Username: {Username} | IP: {ClientIp} | Reason: {Reason} | Result: Failure",
                dto.Username, ctx.Location, ex.Message);
            return this.Fail(ex.Message);
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ctx = _requestContext.GetRequestContext();
        var command = new LoginCommand
        {
            Username = dto.Username,
            Password = dto.Password,
            DeviceType = ctx.DeviceType,
            Browser = ctx.Browser,
            OS = ctx.OS,
            Location = ctx.Location,
            AppVersion = ctx.AppVersion ?? dto.AppVersion
        };

        try
        {
            var result = await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.UserLoginSucceeded,
                "UserId: {UserId} | Username: {Username} | IP: {ClientIp} | Device: {DeviceType} | OS: {OS} | Browser: {Browser} | Method: Password | Result: Success",
                result.User?.Id, dto.Username, ctx.Location, ctx.DeviceType, ctx.OS, ctx.Browser);
            return this.Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLog.Warning(_logger, AppLogEvents.UserLoginFailed,
                "Username: {Username} | IP: {ClientIp} | Device: {DeviceType} | Reason: {Reason} | Result: Failure",
                dto.Username, ctx.Location, ctx.DeviceType, ClassifyLoginFailure(ex.Message));
            return this.ApiUnauthorized(ex.Message);
        }
        catch (Core.Application.Exceptions.MaxSessionsReachedException ex)
        {
            AppLog.Warning(_logger, AppLogEvents.UserLoginFailed,
                "Username: {Username} | IP: {ClientIp} | Reason: MaxSessionsReached | Result: Failure",
                dto.Username, ctx.Location);
            return StatusCode(StatusCodes.Status409Conflict, WebAPI.Models.ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var ctx = _requestContext.GetRequestContext();
        var command = new RefreshTokenCommand
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            AppVersion = ctx.AppVersion ?? dto.AppVersion
        };

        try
        {
            var result = await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.TokenRefreshed,
                "UserId: {UserId} | IP: {ClientIp} | Result: Success",
                result.User?.Id, ctx.Location);
            return this.Success(result);
        }
        catch (UnauthorizedAccessException)
        {
            AppLog.Warning(_logger, AppLogEvents.TokenRefreshFailed,
                "IP: {ClientIp} | Reason: InvalidOrExpiredToken | Result: Failure",
                ctx.Location);
            return this.ApiUnauthorized("Invalid or expired token.");
        }
    }

    private static string ClassifyLoginFailure(string message) =>
        message switch
        {
            var m when m.Contains("disabled", StringComparison.OrdinalIgnoreCase)
                       || m.Contains("suspended", StringComparison.OrdinalIgnoreCase) => "AccountDisabled",
            var m when m.Contains("Guest", StringComparison.OrdinalIgnoreCase) => "GuestNotAllowed",
            _ => "InvalidCredentials"
        };
}
