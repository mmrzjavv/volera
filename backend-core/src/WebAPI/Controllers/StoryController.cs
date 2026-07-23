using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Queries;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class StoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst("userId")?.Value ?? throw new UnauthorizedAccessException());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoryRequestDto dto)
    {
        var storyId = await _mediator.Send(new CreateStoryCommand
        {
            UserId = CurrentUserId,
            Items = dto.Items
        });
        return this.Success(new { storyId });
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed()
    {
        var feed = await _mediator.Send(new GetStoryFeedQuery { UserId = CurrentUserId });
        return this.Success(feed);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserStories(Guid userId)
    {
        var stories = await _mediator.Send(new GetUserStoriesQuery
        {
            ViewerUserId = CurrentUserId,
            TargetUserId = userId
        });
        return this.Success(stories);
    }

    [HttpPost("{storyId:guid}/view")]
    public async Task<IActionResult> MarkViewed(Guid storyId)
    {
        await _mediator.Send(new MarkStoryViewedCommand
        {
            StoryId = storyId,
            ViewerUserId = CurrentUserId
        });
        return this.Success();
    }

    [HttpGet("{storyId:guid}/viewers")]
    public async Task<IActionResult> GetViewers(Guid storyId)
    {
        var viewers = await _mediator.Send(new GetStoryViewersQuery
        {
            StoryId = storyId,
            RequesterId = CurrentUserId
        });
        return this.Success(viewers);
    }

    [HttpDelete("{storyId:guid}")]
    public async Task<IActionResult> Delete(Guid storyId)
    {
        await _mediator.Send(new DeleteStoryCommand
        {
            StoryId = storyId,
            UserId = CurrentUserId
        });
        return this.Success();
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        await _mediator.Send(new DeleteStoryItemCommand
        {
            ItemId = itemId,
            UserId = CurrentUserId
        });
        return this.Success();
    }

    [HttpPost("items/{itemId:guid}/reply")]
    public async Task<IActionResult> Reply(Guid itemId, [FromBody] ReplyToStoryRequestDto dto)
    {
        var messageId = await _mediator.Send(new ReplyToStoryItemCommand
        {
            ItemId = itemId,
            SenderId = CurrentUserId,
            Content = dto.Content
        });
        return this.Success(new { messageId });
    }
}
