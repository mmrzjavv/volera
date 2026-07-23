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
/// SignalR hub for company widget clients. Auth via company client token in query string.
/// </summary>
[AllowAnonymous]
public class CompanyWidgetHub : Hub
{
    private readonly ICompanyWidgetTokenService _widgetTokenService;
    private readonly ICompanyClientConnectionManager _clientConnectionManager;
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageRepository _messageRepository;
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IMediator _mediator;

    public CompanyWidgetHub(
        ICompanyWidgetTokenService widgetTokenService,
        ICompanyClientConnectionManager clientConnectionManager,
        IConnectionManager connectionManager,
        IMessageRepository messageRepository,
        ISystemLimitRepository systemLimitRepository,
        IMediator mediator)
    {
        _widgetTokenService = widgetTokenService;
        _clientConnectionManager = clientConnectionManager;
        _connectionManager = connectionManager;
        _messageRepository = messageRepository;
        _systemLimitRepository = systemLimitRepository;
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            throw new HubException("Company client token is required. Pass access_token in query string.");

        var client = await _widgetTokenService.ValidateCompanyClientTokenAsync(token);
        if (client == null)
            throw new HubException("Invalid or expired company client token.");

        var userIdString = client.UserId.ToString();
        _clientConnectionManager.RegisterConnection(Context.ConnectionId, client.UserId);
        _connectionManager.RegisterConnection(Context.ConnectionId, userIdString);
        await Groups.AddToGroupAsync(Context.ConnectionId, "client_" + userIdString);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _clientConnectionManager.UnregisterConnection(Context.ConnectionId);
        _connectionManager.UnregisterConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string content, Guid? replyToMessageId = null, string? attachmentUrl = null, string? attachmentType = null)
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
            throw new HubException("Company client token is required.");

        var client = await _widgetTokenService.ValidateCompanyClientTokenAsync(token);
        if (client == null)
            throw new HubException("Invalid or expired company client token.");

        var limitRecord = await _systemLimitRepository.GetByKeyAsync(LimitKeys.MaxGuestMessagesPerMinute);
        var maxPerMinute = limitRecord != null ? (int)limitRecord.Value : 10;
        var since = DateTime.UtcNow.AddMinutes(-1);
        var count = await _messageRepository.GetCountBySenderSinceAsync(client.UserId, since);
        if (count >= maxPerMinute)
            throw new HubException($"Rate limit exceeded. Maximum {maxPerMinute} messages per minute.");

        var command = new SendCompanyMessageCommand
        {
            ClientToken = token,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            AttachmentUrl = attachmentUrl,
            AttachmentType = attachmentType
        };
        await _mediator.Send(command);
    }
}
