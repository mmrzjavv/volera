using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
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
        _logger.LogInformation("Support login attempt for username {Username}.", request.Username);

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
                return this.ApiUnauthorized("Invalid username or password.");
            _logger.LogInformation("Support login successful for username {Username}.", request.Username);
            return this.Success(result);
        }

        var byUsernameCommand = new SupportUserLoginByUsernameCommand
        {
            Username = request.Username,
            Password = request.Password
        };
        var byUsernameResult = await _mediator.Send(byUsernameCommand, cancellationToken);
        if (byUsernameResult == null)
            return this.ApiUnauthorized("Invalid username or password.");
        _logger.LogInformation("Support login successful for username {Username}.", request.Username);
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
            return this.ApiUnauthorized("Invalid or expired token.");
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
