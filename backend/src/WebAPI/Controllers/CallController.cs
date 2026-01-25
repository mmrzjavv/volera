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
public class CallController : ControllerBase
{
    private readonly IMediator _mediator;

    public CallController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> InitiateCall([FromBody] InitiateCallDto dto)
    {
        var callerId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new InitiateCallCommand
        {
            CallerId = callerId,
            ReceiverId = dto.ReceiverId
        };

        var callId = await _mediator.Send(command);
        return Ok(new { CallId = callId });
    }

    [HttpPost("{callId}/accept")]
    public async Task<IActionResult> AcceptCall(Guid callId)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new AcceptCallCommand
        {
            CallId = callId,
            UserId = userId
        };

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{callId}/reject")]
    public async Task<IActionResult> RejectCall(Guid callId)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new RejectCallCommand
        {
            CallId = callId,
            UserId = userId
        };

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{callId}/end")]
    public async Task<IActionResult> EndCall(Guid callId)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new EndCallCommand
        {
            CallId = callId,
            UserId = userId
        };

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetCallHistory()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetCallsByUserIdQuery { UserId = userId };
        var calls = await _mediator.Send(query);
        return Ok(calls);
    }
}