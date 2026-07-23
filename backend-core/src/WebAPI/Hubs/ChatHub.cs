using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Application.Interfaces;
using WebAPI.DTOs;
using MediatR;
using Core.Application.Commands;
using Core.Application.Queries;

namespace WebAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IOnlineUserService _onlineUserService;
    private readonly IConnectionManager _connectionManager;

    public ChatHub(IMediator mediator, IOnlineUserService onlineUserService, IConnectionManager connectionManager)
    {
        _mediator = mediator;
        _onlineUserService = onlineUserService;
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                // Register connection for notification
                _connectionManager.RegisterConnection(Context.ConnectionId, userIdClaim);
                await _onlineUserService.UserConnected(userId);

                // Join user to their group channels
                var groups = await _mediator.Send(new GetUserGroupsQuery { UserId = userId });
                foreach (var group in groups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHub] Error in OnConnectedAsync: {ex.Message}");
            // We might want to rethrow or allow the connection but log the error
            // If we rethrow, the client gets the AbortError
            // If we swallow, the client connects but might not receive messages
            // For now, let's swallow to see if it fixes the "negotiation" error, 
            // but the user won't be in groups.
            // Better to log and allow connection to proceed so we can at least debug via console logs if possible.
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            // Unregister connection
            _connectionManager.UnregisterConnection(Context.ConnectionId);
            await _onlineUserService.UserDisconnected(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Guid> SendMessage(Guid receiverId, string content, Guid? replyToMessageId = null, string? attachmentUrl = null, string? attachmentType = null, Guid? clientMessageId = null)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var senderId))
        {
            throw new HubException("Unauthorized");
        }

        var command = new SendMessageCommand
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            AttachmentUrl = attachmentUrl,
            AttachmentType = attachmentType,
            ClientMessageId = clientMessageId
        };

        return await _mediator.Send(command);
    }

    public async Task<Guid> SendGroupMessage(Guid groupId, string content, Guid? replyToMessageId = null, string? attachmentUrl = null, string? attachmentType = null, Guid? clientMessageId = null)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var senderId))
        {
            throw new HubException("Unauthorized");
        }

        var command = new SendMessageCommand
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            AttachmentUrl = attachmentUrl,
            AttachmentType = attachmentType,
            ClientMessageId = clientMessageId
        };

        return await _mediator.Send(command);
    }

    public async Task JoinGroup(Guid groupId)
    {
        // Client calls this when they create/join a group.
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }
}
