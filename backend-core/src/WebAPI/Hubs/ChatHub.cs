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
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMediator mediator,
        IOnlineUserService onlineUserService,
        IConnectionManager connectionManager,
        ILogger<ChatHub> logger)
    {
        _mediator = mediator;
        _onlineUserService = onlineUserService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userIdClaim = Context.User?.FindFirst("userId")?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                _connectionManager.RegisterConnection(Context.ConnectionId, userIdClaim);
                await _onlineUserService.UserConnected(userId);

                var groups = await _mediator.Send(new GetUserGroupsQuery { UserId = userId });
                foreach (var group in groups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ChatHubConnectFailed | ConnectionId: {ConnectionId} | Error: {ErrorType} | Result: Failure",
                Context.ConnectionId, ex.GetType().Name);
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
