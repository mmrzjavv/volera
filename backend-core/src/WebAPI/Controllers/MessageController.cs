using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Queries;
using Core.Application.Commands;
using WebAPI.DTOs;
using Core.Application.Logging;
using WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IMediator mediator, ILogger<MessageController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [EnableRateLimiting("MessageSend")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
            return this.ApiUnauthorized();

        if (request.ReceiverId.HasValue == request.GroupId.HasValue)
            return this.Fail("Provide exactly one of receiverId or groupId.");

        var command = new SendMessageCommand
        {
            SenderId = currentUserId.Value,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Content = request.Content ?? string.Empty,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType,
            ReplyToMessageId = request.ReplyToMessageId,
            ClientMessageId = request.ClientMessageId,
            SendAsChannelId = request.SendAsChannelId
        };

        var messageId = await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.MessageSent,
            "UserId: {UserId} | MessageId: {MessageId} | ReceiverId: {ReceiverId} | GroupId: {GroupId} | HasAttachment: {HasAttachment} | Result: Success",
            currentUserId, messageId, request.ReceiverId, request.GroupId, !string.IsNullOrEmpty(request.AttachmentUrl));
        return this.Success(new { id = messageId, clientMessageId = request.ClientMessageId });
    }

    [HttpGet("sync")]
    public async Task<IActionResult> Sync(
        [FromQuery] Guid? peerUserId,
        [FromQuery] Guid? groupId,
        [FromQuery] DateTime? afterSentAt,
        [FromQuery] Guid? afterId,
        [FromQuery] int limit = 50)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
            return this.ApiUnauthorized();

        var result = await _mediator.Send(new SyncMessagesQuery
        {
            CurrentUserId = currentUserId.Value,
            PeerUserId = peerUserId,
            GroupId = groupId,
            AfterSentAt = afterSentAt,
            AfterId = afterId,
            Limit = limit
        });
        return this.Success(result);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentChats()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetRecentChatsQuery
        {
            UserId = currentUserId.Value
        };
        var chats = await _mediator.Send(query);
        return this.Success(chats);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetTotalCount()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetTotalMessagesCountQuery
        {
            UserId = currentUserId.Value
        };

        var count = await _mediator.Send(query);
        return this.Success(new { count });
    }

    [HttpPost("mark-read/{senderId}")]
    public async Task<IActionResult> MarkAsRead(Guid senderId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new MarkMessagesAsReadCommand
        {
            UserId = currentUserId.Value,
            SenderId = senderId
        };
        await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.MessagesMarkedRead,
            "UserId: {UserId} | SenderId: {SenderId} | Result: Success",
            currentUserId, senderId);
        return this.Success();
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadCounts()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetUnreadCountsQuery
        {
            UserId = currentUserId.Value
        };

        var counts = await _mediator.Send(query);
        return this.Success(counts);
    }

    [HttpGet("saved")]
    public async Task<IActionResult> GetSavedMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetSavedMessagesQuery(currentUserId.Value, page, pageSize);
        var result = await _mediator.Send(query);
        return this.Success(result);
    }

    [HttpPost("{messageId:guid}/save")]
    public async Task<IActionResult> SaveMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new SaveMessageCommand(currentUserId.Value, messageId);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpDelete("{messageId:guid}/save")]
    public async Task<IActionResult> UnsaveMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new UnsaveMessageCommand(currentUserId.Value, messageId);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetConversation(Guid userId, [FromQuery] DateTime? before, [FromQuery] int limit = 20)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var query = new GetMessagesQuery
        {
            CurrentUserId = currentUserId.Value,
            UserId = userId,
            Before = before,
            Limit = limit
        };
        var messages = await _mediator.Send(query);
        return this.Success(messages);
    }

    [HttpPatch("{messageId}")]
    public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new EditMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value,
            NewContent = request.Content
        };

        try
        {
            var result = await _mediator.Send(command);
            if (!result)
                return this.ApiNotFound("Message not found");
            AppLog.Info(_logger, AppLogEvents.MessageEdited,
                "UserId: {UserId} | MessageId: {MessageId} | Result: Success",
                currentUserId, messageId);
            return this.Success();
        }
        catch (UnauthorizedAccessException)
        {
            AppLog.Warning(_logger, AppLogEvents.AuthorizationDenied,
                "UserId: {UserId} | Action: EditMessage | MessageId: {MessageId} | Result: Failure",
                currentUserId, messageId);
            return this.ApiForbid("You are not allowed to edit this message");
        }
        catch (InvalidOperationException ex)
        {
            return this.Fail(ex.Message);
        }
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new DeleteMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };

        try
        {
            var result = await _mediator.Send(command);
            if (!result)
                return this.ApiNotFound("Message not found");
            AppLog.Info(_logger, AppLogEvents.MessageDeleted,
                "UserId: {UserId} | MessageId: {MessageId} | Result: Success",
                currentUserId, messageId);
            return this.Success();
        }
        catch (UnauthorizedAccessException)
        {
            AppLog.Warning(_logger, AppLogEvents.AuthorizationDenied,
                "UserId: {UserId} | Action: DeleteMessage | MessageId: {MessageId} | Result: Failure",
                currentUserId, messageId);
            return this.ApiForbid("You are not allowed to delete this message");
        }
    }

    [HttpPost("{messageId:guid}/reaction")]
    public async Task<IActionResult> AddOrUpdateReaction(Guid messageId, [FromBody] ReactionRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new AddOrUpdateReactionCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value,
            Emoji = request.Emoji
        };
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpDelete("{messageId:guid}/reaction")]
    public async Task<IActionResult> RemoveReaction(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new RemoveReactionCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };
        await _mediator.Send(command);
        return this.Success();
    }

    public class ForwardMessageRequest
    {
        public Guid? ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
    }

    [HttpPost("{messageId:guid}/forward")]
    public async Task<IActionResult> ForwardMessage(Guid messageId, [FromBody] ForwardMessageRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new ForwardMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId
        };
        var newMessageId = await _mediator.Send(command);
        AppLog.Info(_logger, AppLogEvents.MessageForwarded,
            "UserId: {UserId} | SourceMessageId: {MessageId} | NewMessageId: {NewMessageId} | ReceiverId: {ReceiverId} | GroupId: {GroupId} | Result: Success",
            currentUserId, messageId, newMessageId, request.ReceiverId, request.GroupId);
        return this.Success(new { messageId = newMessageId });
    }

    [HttpPost("{messageId:guid}/pin")]
    public async Task<IActionResult> PinMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new PinMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };
        await _mediator.Send(command);
        return this.Success();
    }

    /// <summary>
    /// Removes a chat from the user's recent list. For direct chats: hides the chat.
    /// For group chats: leaves the group. Telegram-style - call after undo timeout.
    /// </summary>
    [HttpDelete("chat")]
    public async Task<IActionResult> RemoveChatFromRecent([FromQuery] Guid? userId, [FromQuery] Guid? groupId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        if (!userId.HasValue && !groupId.HasValue)
        {
            return this.Fail("Provide userId or groupId.");
        }

        var command = new RemoveChatFromRecentCommand
        {
            CurrentUserId = currentUserId.Value,
            OtherUserId = userId,
            GroupId = groupId
        };

        try
        {
            await _mediator.Send(command);
            AppLog.Info(_logger, AppLogEvents.ChatRemoved,
                "UserId: {UserId} | OtherUserId: {OtherUserId} | GroupId: {GroupId} | Result: Success",
                currentUserId, userId, groupId);
            return this.Success();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound("Chat not found");
        }
    }

    [HttpDelete("{messageId:guid}/pin")]
    public async Task<IActionResult> UnpinMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            return this.ApiUnauthorized();
        }

        var command = new UnpinMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };
        await _mediator.Send(command);
        return this.Success();
    }
}
