using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Queries;
using Core.Application.Commands;
using WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/Channel")]
public class ChannelController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChannelController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChannelRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();

        var id = await _mediator.Send(new CreateChannelCommand
        {
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            PublicUsername = request.PublicUsername,
            CreatorId = userId.Value
        });
        var details = await _mediator.Send(new GetChannelDetailsQuery { ChannelId = id, CurrentUserId = userId.Value });
        return this.Success(new { channelId = id, inviteCode = details.InviteCode });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine()
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var list = await _mediator.Send(new GetMyChannelsQuery { UserId = userId.Value });
        return this.Success(list);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        var list = await _mediator.Send(new SearchPublicChannelsQuery { Query = q ?? string.Empty, Limit = limit });
        return this.Success(list);
    }

    [HttpGet("u/{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> ByUsername(string username)
    {
        var userId = this.GetCurrentUserId();
        try
        {
            var details = await _mediator.Send(new GetChannelByUsernameQuery
            {
                PublicUsername = username,
                CurrentUserId = userId
            });
            return this.Success(details);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Channel not found");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        try
        {
            var details = await _mediator.Send(new GetChannelDetailsQuery { ChannelId = id, CurrentUserId = userId.Value });
            return this.Success(details);
        }
        catch (KeyNotFoundException) { return this.ApiNotFound("Channel not found"); }
        catch (UnauthorizedAccessException ex) { return this.ApiForbid(ex.Message); }
    }

    [HttpPost("{id:guid}/subscribe")]
    public async Task<IActionResult> Subscribe(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new SubscribeToChannelCommand { ChannelId = id, UserId = userId.Value });
        return this.Success(new { subscribed = true });
    }

    [HttpDelete("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new LeaveChannelCommand { ChannelId = id, UserId = userId.Value });
        return this.Success(new { left = true });
    }

    [HttpPost("{id:guid}/invite-link")]
    public async Task<IActionResult> InviteLink(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var code = await _mediator.Send(new GenerateChannelInviteLinkCommand { ChannelId = id, RequestingUserId = userId.Value });
        return this.Success(new { inviteCode = code });
    }

    [HttpGet("invite/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> InvitePreview(string code)
    {
        try
        {
            var details = await _mediator.Send(new GetChannelInvitePreviewQuery { InviteCode = code });
            return this.Success(details);
        }
        catch (KeyNotFoundException) { return this.ApiNotFound("Invite not found"); }
    }

    [HttpPost("join/{code}")]
    public async Task<IActionResult> JoinByInvite(string code)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var channelId = await _mediator.Send(new JoinChannelByInviteCommand { InviteCode = code, UserId = userId.Value });
        return this.Success(new { channelId });
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateChannelProfileRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new UpdateChannelProfileCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            Name = request.Name,
            Description = request.Description,
            ProfilePictureUrl = request.ProfilePictureUrl
        });
        return this.Success(new { updated = true });
    }

    [HttpPut("{id:guid}/visibility")]
    public async Task<IActionResult> SetVisibility(Guid id, [FromBody] SetChannelVisibilityRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new SetChannelVisibilityCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            IsPublic = request.IsPublic,
            PublicUsername = request.PublicUsername
        });
        return this.Success(new { updated = true });
    }

    [HttpPost("{id:guid}/admins")]
    public async Task<IActionResult> SetAdmin(Guid id, [FromBody] SetChannelAdminRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new SetChannelAdminCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            TargetUserId = request.UserId,
            CanPost = request.CanPost,
            CanEditMessages = request.CanEditMessages,
            CanDeleteMessages = request.CanDeleteMessages,
            CanManageSubscribers = request.CanManageSubscribers,
            CanChangeInfo = request.CanChangeInfo,
            CanAddAdmins = request.CanAddAdmins
        });
        return this.Success(new { updated = true });
    }

    [HttpDelete("{id:guid}/admins/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveAdmin(Guid id, Guid targetUserId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new RemoveChannelAdminCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            TargetUserId = targetUserId
        });
        return this.Success(new { removed = true });
    }

    [HttpPost("{id:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new TransferChannelOwnershipCommand
        {
            ChannelId = id,
            CurrentOwnerId = userId.Value,
            NewOwnerId = request.NewOwnerId
        });
        return this.Success(new { transferred = true });
    }

    [HttpPost("{id:guid}/subscribers")]
    public async Task<IActionResult> AddSubscriber(Guid id, [FromBody] AddSubscriberRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new AddChannelSubscriberCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            UserId = request.UserId
        });
        return this.Success(new { added = true });
    }

    [HttpDelete("{id:guid}/subscribers/{targetUserId:guid}")]
    public async Task<IActionResult> RemoveSubscriber(Guid id, Guid targetUserId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new RemoveChannelSubscriberCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            UserId = targetUserId
        });
        return this.Success(new { removed = true });
    }

    [HttpGet("{id:guid}/subscribers")]
    public async Task<IActionResult> Subscribers(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var list = await _mediator.Send(new GetChannelSubscribersQuery
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            Page = page,
            PageSize = pageSize
        });
        return this.Success(list);
    }

    [HttpPut("{id:guid}/signatures")]
    public async Task<IActionResult> ToggleSignatures(Guid id, [FromBody] ToggleSignaturesRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new ToggleChannelSignaturesCommand
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            Enabled = request.Enabled
        });
        return this.Success(new { enabled = request.Enabled });
    }

    [HttpPost("{id:guid}/views")]
    public async Task<IActionResult> RecordViews(Guid id, [FromBody] RecordViewsRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        _ = id;
        var count = await _mediator.Send(new RecordChannelMessageViewsCommand
        {
            UserId = userId.Value,
            MessageIds = request.MessageIds ?? []
        });
        return this.Success(new { recorded = count });
    }

    [HttpGet("{id:guid}/analytics")]
    public async Task<IActionResult> Analytics(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var data = await _mediator.Send(new GetChannelAnalyticsQuery { ChannelId = id, RequestingUserId = userId.Value });
        return this.Success(data);
    }

    [HttpPost("{id:guid}/discussion")]
    public async Task<IActionResult> LinkDiscussion(Guid id, [FromBody] LinkDiscussionRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new LinkDiscussionGroupCommand
        {
            ChannelId = id,
            DiscussionGroupId = request.DiscussionGroupId,
            RequestingUserId = userId.Value
        });
        return this.Success(new { linked = true });
    }

    [HttpDelete("{id:guid}/discussion")]
    public async Task<IActionResult> UnlinkDiscussion(Guid id)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new UnlinkDiscussionGroupCommand { ChannelId = id, RequestingUserId = userId.Value });
        return this.Success(new { unlinked = true });
    }

    [HttpPost("{id:guid}/suggestions")]
    public async Task<IActionResult> Suggest(Guid id, [FromBody] SuggestPostRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var suggestionId = await _mediator.Send(new SuggestChannelPostCommand
        {
            ChannelId = id,
            FromUserId = userId.Value,
            Content = request.Content,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType
        });
        return this.Success(new { suggestionId });
    }

    [HttpGet("{id:guid}/suggestions")]
    public async Task<IActionResult> ListSuggestions(Guid id, [FromQuery] string? status = null)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var list = await _mediator.Send(new ListSuggestedPostsQuery
        {
            ChannelId = id,
            RequestingUserId = userId.Value,
            Status = status
        });
        return this.Success(list);
    }

    [HttpPost("suggestions/{suggestionId:guid}/accept")]
    public async Task<IActionResult> AcceptSuggestion(Guid suggestionId)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        var messageId = await _mediator.Send(new AcceptSuggestedPostCommand
        {
            SuggestedPostId = suggestionId,
            RequestingUserId = userId.Value
        });
        return this.Success(new { messageId });
    }

    [HttpPost("suggestions/{suggestionId:guid}/reject")]
    public async Task<IActionResult> RejectSuggestion(Guid suggestionId, [FromBody] RejectSuggestionRequest? request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new RejectSuggestedPostCommand
        {
            SuggestedPostId = suggestionId,
            RequestingUserId = userId.Value,
            AdminNote = request?.AdminNote
        });
        return this.Success(new { rejected = true });
    }

    [HttpPost("suggestions/{suggestionId:guid}/schedule")]
    public async Task<IActionResult> ScheduleSuggestion(Guid suggestionId, [FromBody] ScheduleSuggestionRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId is null) return this.ApiUnauthorized();
        await _mediator.Send(new ScheduleSuggestedPostCommand
        {
            SuggestedPostId = suggestionId,
            RequestingUserId = userId.Value,
            ScheduledAt = request.ScheduledAt,
            AdminNote = request.AdminNote
        });
        return this.Success(new { scheduled = true });
    }
}

public class CreateChannelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
}

public class UpdateChannelProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class SetChannelVisibilityRequest
{
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
}

public class SetChannelAdminRequest
{
    public Guid UserId { get; set; }
    public bool CanPost { get; set; } = true;
    public bool CanEditMessages { get; set; } = true;
    public bool CanDeleteMessages { get; set; } = true;
    public bool CanManageSubscribers { get; set; } = true;
    public bool CanChangeInfo { get; set; } = true;
    public bool CanAddAdmins { get; set; }
}

public class TransferOwnershipRequest
{
    public Guid NewOwnerId { get; set; }
}

public class AddSubscriberRequest
{
    public Guid UserId { get; set; }
}

public class ToggleSignaturesRequest
{
    public bool Enabled { get; set; }
}

public class RecordViewsRequest
{
    public List<Guid>? MessageIds { get; set; }
}

public class LinkDiscussionRequest
{
    public Guid DiscussionGroupId { get; set; }
}

public class SuggestPostRequest
{
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
}

public class RejectSuggestionRequest
{
    public string? AdminNote { get; set; }
}

public class ScheduleSuggestionRequest
{
    public DateTime ScheduledAt { get; set; }
    public string? AdminNote { get; set; }
}
