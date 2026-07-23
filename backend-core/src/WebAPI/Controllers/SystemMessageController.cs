using Core.Application.Commands.SystemMessages;
using Core.Application.Queries.SystemMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Extensions;
using System.Security.Claims;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/system-messages")]
public class SystemMessageController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemMessageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSystemMessageRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new CreateSystemMessageCommand(userId, request.Title, request.Content, request.ExpiresAt);
        var id = await _mediator.Send(command);

        return this.SuccessCreated(nameof(GetActive), new { id }, new { id });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var query = new GetActiveSystemMessagesQuery(userId);
        var messages = await _mediator.Send(query);

        var response = messages
            .Select(m => new SystemMessageResponse(
                m.Id,
                m.Title,
                m.Content,
                m.CreatedAt,
                m.ExpiresAt,
                m.IsActive,
                m.IsRead))
            .ToList();

        return this.Success(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSystemMessageRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new UpdateSystemMessageCommand(id, userId, request.Title, request.Content, request.ExpiresAt);
        await _mediator.Send(command);

        return this.Success();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new DeleteSystemMessageCommand(id, userId);
        await _mediator.Send(command);

        return this.Success();
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());
        var command = new MarkSystemMessageReadCommand(id, userId);
        await _mediator.Send(command);

        return this.Success();
    }
}
