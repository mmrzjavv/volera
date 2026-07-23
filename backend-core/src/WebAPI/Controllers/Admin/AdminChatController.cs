using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Administration.Commands;
using Core.Application.Administration.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/chats")]
[Authorize(Policy = "Admin")]
public class AdminChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? type = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _mediator.Send(new GetAdminChatListQuery(page, pageSize, searchTerm, type));
        return this.Success(result);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey([FromRoute] string key)
    {
        var result = await _mediator.Send(new GetChatByKeyQuery(key));
        return result != null ? this.Success(result) : this.ApiNotFound("Conversation not found.");
    }

    [HttpGet("{key}/messages")]
    public async Task<IActionResult> GetMessages(
        [FromRoute] string key,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var result = await _mediator.Send(new GetAdminConversationQuery(key, limit, before));
        return this.Success(result);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> PurgeConversation([FromRoute] string key)
    {
        var adminId = this.GetCurrentUserId();
        if (!adminId.HasValue) return this.ApiUnauthorized();
        var deleted = await _mediator.Send(new AdminPurgeConversationCommand(key, adminId.Value));
        return this.Success(new { deleted });
    }
}
