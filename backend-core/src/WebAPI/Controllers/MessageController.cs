using Microsoft.AspNetCore.Mvc;
using MediatR;
using Core.Application.Queries;
using Core.Application.Commands;
using WebAPI.DTOs;
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
            _logger.LogWarning("Unauthorized attempt to fetch recent chats. No valid user id in claims.");
            return this.ApiUnauthorized();
        }

        var query = new GetRecentChatsQuery
        {
            UserId = currentUserId.Value
        };

        _logger.LogInformation("User {UserId} is fetching recent chats.", currentUserId);
        var chats = await _mediator.Send(query);
        _logger.LogInformation("User {UserId} fetched recent chats successfully.", currentUserId);
        return this.Success(chats);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetTotalCount()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to get total message count. No valid user id in claims.");
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
            _logger.LogWarning("Unauthorized attempt to mark messages as read from sender {SenderId}.", senderId);
            return this.ApiUnauthorized();
        }

        var command = new MarkMessagesAsReadCommand
        {
            UserId = currentUserId.Value,
            SenderId = senderId
        };

        _logger.LogInformation("User {UserId} is marking messages as read from sender {SenderId}.", currentUserId, senderId);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadCounts()
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to get unread counts. No valid user id in claims.");
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
            _logger.LogWarning("Unauthorized attempt to get saved messages for page {Page}, pageSize {PageSize}.", page, pageSize);
            return this.ApiUnauthorized();
        }

        var query = new GetSavedMessagesQuery(currentUserId.Value, page, pageSize);
        _logger.LogInformation("User {UserId} is fetching saved messages. Page: {Page}, PageSize: {PageSize}.", currentUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return this.Success(result);
    }

    [HttpPost("{messageId:guid}/save")]
    public async Task<IActionResult> SaveMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to save message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new SaveMessageCommand(currentUserId.Value, messageId);
        _logger.LogInformation("User {UserId} is saving message {MessageId}.", currentUserId, messageId);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpDelete("{messageId:guid}/save")]
    public async Task<IActionResult> UnsaveMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to unsave message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new UnsaveMessageCommand(currentUserId.Value, messageId);
        _logger.LogInformation("User {UserId} is unsaving message {MessageId}.", currentUserId, messageId);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetConversation(Guid userId, [FromQuery] DateTime? before, [FromQuery] int limit = 20)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to get conversation with user {OtherUserId}.", userId);
            return this.ApiUnauthorized();
        }

        var query = new GetMessagesQuery
        {
            CurrentUserId = currentUserId.Value,
            UserId = userId,
            Before = before,
            Limit = limit
        };

        _logger.LogInformation("User {UserId} is fetching conversation with {OtherUserId}. Before: {Before}, Limit: {Limit}.", currentUserId, userId, before, limit);
        var messages = await _mediator.Send(query);
        return this.Success(messages);
    }

    [HttpPatch("{messageId}")]
    public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to edit message {MessageId}.", messageId);
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
            _logger.LogInformation("User {UserId} is editing message {MessageId}.", currentUserId, messageId);
            var result = await _mediator.Send(command);
            if (!result)
                return this.ApiNotFound("Message not found");
            return this.Success();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("User {UserId} attempted to edit message {MessageId} but was forbidden.", currentUserId, messageId);
            return this.ApiForbid("You are not allowed to edit this message");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while editing message {MessageId} for user {UserId}.", messageId, currentUserId);
            return this.Fail(ex.Message);
        }
    }

    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to delete message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new DeleteMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };

        try
        {
            _logger.LogInformation("User {UserId} is deleting message {MessageId}.", currentUserId, messageId);
            var result = await _mediator.Send(command);
            if (!result)
                return this.ApiNotFound("Message not found");
            return this.Success();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("User {UserId} attempted to delete message {MessageId} but was forbidden.", currentUserId, messageId);
            return this.ApiForbid("You are not allowed to delete this message");
        }
    }

    [HttpPost("{messageId:guid}/reaction")]
    public async Task<IActionResult> AddOrUpdateReaction(Guid messageId, [FromBody] ReactionRequest request)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to add/update reaction for message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new AddOrUpdateReactionCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value,
            Emoji = request.Emoji
        };

        _logger.LogInformation("User {UserId} is adding/updating reaction on message {MessageId} with emoji {Emoji}.", currentUserId, messageId, request.Emoji);
        await _mediator.Send(command);
        return this.Success();
    }

    [HttpDelete("{messageId:guid}/reaction")]
    public async Task<IActionResult> RemoveReaction(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to remove reaction for message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new RemoveReactionCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };

        _logger.LogInformation("User {UserId} is removing reaction from message {MessageId}.", currentUserId, messageId);
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
            _logger.LogWarning("Unauthorized attempt to forward message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new ForwardMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId
        };

        _logger.LogInformation("User {UserId} is forwarding message {MessageId} to Receiver {ReceiverId} / Group {GroupId}.",
            currentUserId,
            messageId,
            request.ReceiverId,
            request.GroupId);
        var newMessageId = await _mediator.Send(command);
        return this.Success(new { messageId = newMessageId });
    }

    [HttpPost("{messageId:guid}/pin")]
    public async Task<IActionResult> PinMessage(Guid messageId)
    {
        var currentUserId = this.GetCurrentUserId();
        if (currentUserId is null)
        {
            _logger.LogWarning("Unauthorized attempt to pin message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new PinMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };

        _logger.LogInformation("User {UserId} is pinning message {MessageId}.", currentUserId, messageId);
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
            _logger.LogWarning("Unauthorized attempt to remove chat.");
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
            _logger.LogInformation("User {UserId} is removing chat (userId={OtherUserId}, groupId={GroupId}).", currentUserId, userId, groupId);
            await _mediator.Send(command);
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
            _logger.LogWarning("Unauthorized attempt to unpin message {MessageId}.", messageId);
            return this.ApiUnauthorized();
        }

        var command = new UnpinMessageCommand
        {
            MessageId = messageId,
            UserId = currentUserId.Value
        };

        _logger.LogInformation("User {UserId} is unpinning message {MessageId}.", currentUserId, messageId);
        await _mediator.Send(command);
        return this.Success();
    }
}
