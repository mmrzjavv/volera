using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var command = new RegisterUserCommand
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            PhoneNumber = dto.PhoneNumber,
            Password = dto.Password
        };

        var userId = await _mediator.Send(command);
        return Ok(new { UserId = userId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var command = new LoginCommand
        {
            Username = dto.Username,
            Password = dto.Password
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}