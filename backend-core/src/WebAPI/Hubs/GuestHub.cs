using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Application.Interfaces;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using MediatR;

namespace WebAPI.Hubs;

/// <summary>
/// Dedicated SignalR hub for guests; auth via guest token in query string; isolates guest real-time flow from JWT-only ChatHub.
/// </summary>
[AllowAnonymous]
public class GuestHub : Hub
{
    private readonly IGuestTokenService _guestTokenService;
    private readonly IGuestConnectionManager _guestConnectionManager;
    private readonly IConnectionManager _connectionManager;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IMediator _mediator;

    public GuestHub(
        IGuestTokenService guestTokenService,
        IGuestConnectionManager guestConnectionManager,
        IConnectionManager connectionManager,
        IAppSettingRepository appSettingRepository,
        IMessageRepository messageRepository,
        ISystemLimitRepository systemLimitRepository,
        IMediator mediator)
    {
        _guestTokenService = guestTokenService;
        _guestConnectionManager = guestConnectionManager;
        _connectionManager = connectionManager;
        _appSettingRepository = appSettingRepository;
        _messageRepository = messageRepository;
        _systemLimitRepository = systemLimitRepository;
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new HubException("Guest token is required. Pass access_token in query string.");
        }

        var guest = await _guestTokenService.ValidateTokenAsync(token);
        if (guest == null)
        {
            throw new HubException("Invalid or expired guest token.");
        }

        var userIdString = guest.UserId.ToString();
        _guestConnectionManager.RegisterConnection(Context.ConnectionId, guest.UserId);
        _connectionManager.RegisterConnection(Context.ConnectionId, userIdString);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _guestConnectionManager.UnregisterConnection(Context.ConnectionId);
        _connectionManager.UnregisterConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Send a message to the configured guest inbox. Receiver is always the inbox user.</summary>
    public async Task SendMessage(string content, Guid? replyToMessageId = null, string? attachmentUrl = null, string? attachmentType = null)
    {
        var guestUserId = _guestConnectionManager.GetUserIdForConnection(Context.ConnectionId);
        if (!guestUserId.HasValue)
        {
            throw new HubException("Unauthorized. Connect with a valid guest token.");
        }

        var limitRecord = await _systemLimitRepository.GetByKeyAsync(LimitKeys.MaxGuestMessagesPerMinute);
        var maxPerMinute = limitRecord != null ? (int)limitRecord.Value : 10;
        var since = DateTime.UtcNow.AddMinutes(-1);
        var count = await _messageRepository.GetCountBySenderSinceAsync(guestUserId.Value, since);
        if (count >= maxPerMinute)
        {
            throw new HubException($"Rate limit exceeded. Maximum {maxPerMinute} messages per minute for guest chat.");
        }

        var inboxSetting = await _appSettingRepository.GetByKeyAsync(AppSettingKeys.GuestInboxUserId);
        if (inboxSetting == null || !Guid.TryParse(inboxSetting.Value, out var inboxUserId))
        {
            throw new HubException("Guest inbox is not configured.");
        }

        var command = new SendMessageCommand
        {
            SenderId = guestUserId.Value,
            ReceiverId = inboxUserId,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            AttachmentUrl = attachmentUrl,
            AttachmentType = attachmentType
        };

        await _mediator.Send(command);
    }
}
