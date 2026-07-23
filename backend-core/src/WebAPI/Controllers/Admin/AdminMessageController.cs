using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Commands;
using Core.Application.Administration.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/messages")]
[Authorize(Policy = "Admin")]
public class AdminMessageController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminMessageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? content = null,
        [FromQuery] Guid? senderId = null,
        [FromQuery] Guid? groupId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new SearchMessagesQuery(page, pageSize, content, senderId, groupId, dateFrom, dateTo));
        return this.Success(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] AdminEditMessageRequest request)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new AdminEditMessageCommand(id, request.Content, adminId.Value));
        return this.Success();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hardDelete = false)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        await _mediator.Send(new AdminDeleteMessageCommand(id, hardDelete, adminId.Value));
        return this.Success();
    }
}

public record AdminEditMessageRequest(string Content);
