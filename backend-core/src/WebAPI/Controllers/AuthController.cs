using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
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
        _logger.LogInformation("Register request for username {Username}, phone {PhoneNumber}.", dto.Username, dto.PhoneNumber);
        var command = new RegisterUserCommand
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            PhoneNumber = dto.PhoneNumber,
            Password = dto.Password
        };

        var userId = await _mediator.Send(command);
        _logger.LogInformation("User registered successfully. UserId: {UserId}, Username: {Username}.", userId, dto.Username);
        return this.Success(new { userId });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("Login attempt for username {Username}.", dto.Username);
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

        var result = await _mediator.Send(command);
        _logger.LogInformation("Login successful for username {Username}.", dto.Username);
        return this.Success(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        _logger.LogInformation("Refresh token request (no user id in context).");
        var ctx = _requestContext.GetRequestContext();
        var command = new RefreshTokenCommand
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            AppVersion = ctx.AppVersion ?? dto.AppVersion
        };

        var result = await _mediator.Send(command);
        _logger.LogInformation("Refresh token completed successfully.");
        return this.Success(result);
    }
}