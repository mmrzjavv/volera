using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Queries;
using Core.Application.Commands;
using WebAPI.DTOs;
using Core.Application.Logging;
using WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class GroupController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GroupController> _logger;

    public GroupController(IMediator mediator, ILogger<GroupController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new CreateGroupCommand
        {
            Name = request.Name,
            CreatorId = currentUserId.Value,
            MemberIds = request.MemberIds ?? []
        };

        var groupId = await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.GroupCreated,
            "UserId: {UserId} | GroupId: {GroupId} | Name: {GroupName} | MemberCount: {MemberCount} | Result: Success",
            currentUserId, groupId, request.Name, request.MemberIds?.Count ?? 0);
        return this.Success(new { groupId });
    }

    [HttpGet("{groupId}/details")]
    public async Task<IActionResult> GetGroupDetails(Guid groupId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }
        var query = new GetGroupDetailsQuery
        {
            GroupId = groupId,
            CurrentUserId = currentUserId.Value
        };

        try
        {
            var details = await _mediator.Send(query);
            return this.Success(details);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not a member of this group");
        }
    }

    [HttpGet("{groupId}/messages")]
    public async Task<IActionResult> GetGroupMessages(Guid groupId, [FromQuery] DateTime? before, [FromQuery] int limit = 20)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }
        var query = new GetGroupMessagesQuery
        {
            GroupId = groupId,
            CurrentUserId = currentUserId.Value,
            Before = before,
            Limit = limit
        };

        try
        {
            var messages = await _mediator.Send(query);
            return this.Success(messages);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not a member of this group");
        }
    }

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddMemberRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }
        var command = new AddMemberCommand
        {
            GroupId = groupId,
            AdminId = currentUserId.Value,
            MemberId = request.MemberId
        };

        try
        {
            await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.GroupMemberAdded,
                "UserId: {UserId} | GroupId: {GroupId} | MemberId: {MemberId} | Result: Success",
                currentUserId, groupId, request.MemberId);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not allowed to add members to this group");
        }
    }

    [HttpDelete("{groupId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new RemoveMemberCommand
        {
            GroupId = groupId,
            AdminId = currentUserId.Value,
            MemberId = memberId
        };

        try
        {
            await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.GroupMemberRemoved,
                "UserId: {UserId} | GroupId: {GroupId} | MemberId: {MemberId} | Result: Success",
                currentUserId, groupId, memberId);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not allowed to remove members from this group");
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
    }

    [HttpPost("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(Guid groupId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new LeaveGroupCommand
        {
            GroupId = groupId,
            UserId = currentUserId.Value
        };

        try
        {
            await _mediator.Send(command);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
    }

    [HttpPost("{groupId}/change-admin")]
    public async Task<IActionResult> ChangeAdmin(Guid groupId, [FromBody] ChangeGroupAdminRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new ChangeGroupAdminCommand
        {
            GroupId = groupId,
            CurrentAdminId = currentUserId.Value,
            NewAdminId = request.NewAdminId
        };

        try
        {
            await _mediator.Send(command);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not allowed to change admin of this group");
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
    }

    [HttpPut("{groupId}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid groupId, [FromBody] UpdateGroupProfileRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new UpdateGroupProfileCommand
        {
            GroupId = groupId,
            RequestingUserId = currentUserId.Value,
            Name = request.Name,
            Description = request.Description,
            ProfilePictureUrl = request.ProfilePictureUrl
        };

        try
        {
            await _mediator.Send(command);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not allowed to update this group");
        }
    }

    [HttpPost("{groupId}/invite-link")]
    public async Task<IActionResult> GenerateInviteLink(Guid groupId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new GenerateGroupInviteLinkCommand
        {
            GroupId = groupId,
            RequestingUserId = currentUserId.Value
        };

        try
        {
            var inviteCode = await _mediator.Send(command);
            return this.Success(new { inviteCode });
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("You are not allowed to generate invite links for this group");
        }
    }

    [AllowAnonymous]
    [HttpGet("invite/{inviteCode}")]
    public async Task<IActionResult> PreviewInvite(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            return this.Fail("Invite code is required");
        }

        // Anonymous preview: return minimal group info
        var group = await _mediator.Send(new GetGroupByInviteCodeQuery { InviteCode = inviteCode });
        return this.Success(group);
    }

    [HttpPost("join-by-invite/{inviteCode}")]
    public async Task<IActionResult> JoinByInvite(string inviteCode)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new JoinGroupByInviteCommand
        {
            InviteCode = inviteCode,
            UserId = currentUserId.Value
        };

        try
        {
            await _mediator.Send(command);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Invalid invite code");
        }
    }
    [HttpDelete("{groupId}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new DeleteGroupCommand
        {
            GroupId = groupId,
            RequestingUserId = currentUserId.Value
        };

        try
        {
            await _mediator.Send(command);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Group not found");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbid("Only group admins can delete the group");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetUserGroupsQuery { UserId = currentUserId.Value };
        var groups = await _mediator.Send(query);
        return this.Success(groups.Where(g => !g.IsChannel).ToList());
    }
}
