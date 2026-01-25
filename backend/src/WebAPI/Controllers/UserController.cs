using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;
using Core.Application.DTOs;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetUserByIdQuery { UserId = userId };
        var user = await _mediator.Send(query);
        return Ok(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetUsersQuery { ExcludeUserId = userId };
        var users = await _mediator.Send(query);
        return Ok(users);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new UpdateProfileCommand
        {
            UserId = userId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            ProfilePicture = dto.ProfilePicture
        };

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new ChangePasswordCommand
        {
            UserId = userId,
            CurrentPassword = dto.CurrentPassword,
            NewPassword = dto.NewPassword
        };

        await _mediator.Send(command);
        return NoContent();
    }
}