using Core.Application.Interfaces;
using Core.Application.DTOs;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class ChatNotificationService : IMessageNotificationService
{
    private const int MessageSnippetLength = 80;

    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IHubContext<GuestHub> _guestHubContext;
    private readonly IHubContext<SupportHub> _supportHubContext;
    private readonly IHubContext<CompanyWidgetHub> _companyWidgetHubContext;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageReadModelService _messageReadModelService;
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;

    public ChatNotificationService(
        IHubContext<ChatHub> hubContext,
        IHubContext<GuestHub> guestHubContext,
        IHubContext<SupportHub> supportHubContext,
        IHubContext<CompanyWidgetHub> companyWidgetHubContext,
        IPushNotificationService pushNotificationService,
        IConnectionManager connectionManager,
        IMessageReadModelService messageReadModelService,
        IMessageRepository messageRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository)
    {
        _hubContext = hubContext;
        _guestHubContext = guestHubContext;
        _supportHubContext = supportHubContext;
        _companyWidgetHubContext = companyWidgetHubContext;
        _pushNotificationService = pushNotificationService;
        _connectionManager = connectionManager;
        _messageReadModelService = messageReadModelService;
        _messageRepository = messageRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
    }

    private async Task NotifyBranchMessageAsync(Guid messageId, Guid senderId, Guid? targetReceiverUserId, Guid branchId, string content, DateTime sentAt, string? attachmentUrl, string? attachmentType, Guid? replyToMessageId = null, Guid? supportSenderId = null)
    {
        object? replyToMessage = null;
        if (replyToMessageId.HasValue)
        {
            var replyMsg = await _messageRepository.GetByIdAsync(replyToMessageId.Value);
            if (replyMsg != null)
                replyToMessage = new { contentSnippet = ContentSnippet(replyMsg.Content ?? "") };
        }
        var payload = new
        {
            messageId,
            senderId,
            targetReceiverUserId,
            branchId,
            content,
            sentAt,
            attachmentUrl,
            attachmentType,
            replyToMessageId,
            replyToMessage,
            supportSenderId
        };
        await _supportHubContext.Clients.Group("branch_" + branchId).SendAsync("BranchMessage", payload);
        if (targetReceiverUserId.HasValue)
            await _companyWidgetHubContext.Clients.Group("client_" + targetReceiverUserId.Value).SendAsync("ReceiveMessage", payload);
    }

    private static string ContentSnippet(string content, int maxLen = MessageSnippetLength)
    {
        if (string.IsNullOrEmpty(content)) return "";
        return content.Length <= maxLen ? content : content.Substring(0, maxLen - 3) + "...";
    }

    public async Task SendMessage(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string content, DateTime sentAt, string? attachmentUrl, string? attachmentType, Guid? branchId = null, Guid? replyToMessageId = null, Guid? supportSenderId = null)
    {
        if (branchId.HasValue)
        {
            await NotifyBranchMessageAsync(messageId, senderId, receiverId, branchId.Value, content, sentAt, attachmentUrl, attachmentType, replyToMessageId, supportSenderId);
            return;
        }

        var messageDto = await _messageReadModelService.BuildMessageDtoForNotificationAsync(
            messageId,
            senderId,
            receiverId,
            groupId,
            content,
            sentAt,
            attachmentUrl,
            attachmentType);

        if (groupId.HasValue)
        {
            // Send to group via SignalR (online members)
            await _hubContext.Clients.Group(groupId.Value.ToString()).SendAsync("ReceiveMessage", messageDto);

            // Push to offline group members so they get notifications when app is backgrounded/closed
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId.Value);
            var sender = await _userRepository.GetByIdAsync(senderId);
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}".Trim() : "Someone";
            if (string.IsNullOrEmpty(senderName)) senderName = sender?.Username ?? "Someone";

            if (group != null)
            {
                var groupName = group.Name ?? "Group";
                var contentSnippet = ContentSnippet(content);
                foreach (var member in group.Members)
                {
                    if (member.UserId == senderId) continue; // Don't push to sender
                    var memberIdString = member.UserId.ToString();
                    var connections = _connectionManager.GetConnectionsForUser(memberIdString);
                    if (!connections.Any())
                    {
                        await _pushNotificationService.SendPushNotificationAsync(
                            member.UserId,
                            $"New message in {groupName}",
                            $"{senderName}: {contentSnippet}",
                            new
                            {
                                type = "group_message",
                                messageId,
                                groupId = groupId.Value,
                                groupName,
                                senderId,
                                senderName,
                                content = contentSnippet
                            });
                    }
                }
            }
        }
        else if (receiverId.HasValue)
        {
            var receiverIdString = receiverId.Value.ToString();
            var senderIdString = senderId.ToString();

            // Resolve sender name for push and SignalR (1:1)
            var sender = await _userRepository.GetByIdAsync(senderId);
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}".Trim() : "Someone";
            if (string.IsNullOrEmpty(senderName)) senderName = sender?.Username ?? "Someone";
            var isGuestSender = sender != null && sender.Role.IsGuest();
            if (isGuestSender) senderName = string.IsNullOrWhiteSpace(senderName) || senderName.StartsWith("g_", StringComparison.Ordinal) ? "Guest" : senderName;
            var contentSnippet = ContentSnippet(content);

            // Send to receiver via SignalR when connected (use GuestHub for guest users, ChatHub for registered users)
            var receiverConnections = _connectionManager.GetConnectionsForUser(receiverIdString);
            if (receiverConnections.Any())
            {
                var receiverUser = await _userRepository.GetByIdAsync(receiverId.Value);
                if (receiverUser != null && receiverUser.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", messageDto);
                else
                    await _hubContext.Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", messageDto);
            }

            // Push for 1:1 so receiver gets notification (including when sender is Guest)
            var pushTitle = isGuestSender ? "New guest message" : $"New message from {senderName}";
            await _pushNotificationService.SendPushNotificationAsync(
                receiverId.Value,
                pushTitle,
                contentSnippet,
                new
                {
                    type = "message",
                    messageId,
                    senderId,
                    senderName,
                    content = contentSnippet
                });

            // Send back to sender (so they see it in their UI immediately/confirmed; guest senders use GuestHub)
            var senderConnections = _connectionManager.GetConnectionsForUser(senderIdString);
            if (senderConnections.Any())
            {
                var senderUserForHub = await _userRepository.GetByIdAsync(senderId);
                if (senderUserForHub != null && senderUserForHub.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(senderConnections).SendAsync("MessageSent", messageDto);
                else
                    await _hubContext.Clients.Clients(senderConnections).SendAsync("MessageSent", messageDto);
            }
        }
    }

    public async Task NotifyMessageEdited(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string newContent, DateTime editedAt)
    {
        var editDto = new { MessageId = messageId, NewContent = newContent, EditedAt = editedAt, GroupId = groupId, ReceiverId = receiverId };

        if (groupId.HasValue)
        {
            await _hubContext.Clients.Group(groupId.Value.ToString()).SendAsync("MessageEdited", editDto);
        }
        else if (receiverId.HasValue)
        {
            var receiverIdString = receiverId.Value.ToString();
            var senderIdString = senderId.ToString();

            var receiverConnections = _connectionManager.GetConnectionsForUser(receiverIdString);
            if (receiverConnections.Any())
            {
                var receiverUser = await _userRepository.GetByIdAsync(receiverId.Value);
                if (receiverUser != null && receiverUser.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(receiverConnections).SendAsync("MessageEdited", editDto);
                else
                    await _hubContext.Clients.Clients(receiverConnections).SendAsync("MessageEdited", editDto);
            }

            var senderConnections = _connectionManager.GetConnectionsForUser(senderIdString);
            if (senderConnections.Any())
            {
                var senderUser = await _userRepository.GetByIdAsync(senderId);
                if (senderUser != null && senderUser.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(senderConnections).SendAsync("MessageEdited", editDto);
                else
                    await _hubContext.Clients.Clients(senderConnections).SendAsync("MessageEdited", editDto);
            }
        }
    }

    public async Task NotifyMessageDeleted(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, DateTime deletedAt)
    {
        var deleteDto = new { MessageId = messageId, DeletedAt = deletedAt, GroupId = groupId, ReceiverId = receiverId };

        if (groupId.HasValue)
        {
            await _hubContext.Clients.Group(groupId.Value.ToString()).SendAsync("MessageDeleted", deleteDto);
        }
        else if (receiverId.HasValue)
        {
            var receiverIdString = receiverId.Value.ToString();
            var senderIdString = senderId.ToString();

            var receiverConnections = _connectionManager.GetConnectionsForUser(receiverIdString);
            if (receiverConnections.Any())
            {
                var receiverUser = await _userRepository.GetByIdAsync(receiverId.Value);
                if (receiverUser != null && receiverUser.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(receiverConnections).SendAsync("MessageDeleted", deleteDto);
                else
                    await _hubContext.Clients.Clients(receiverConnections).SendAsync("MessageDeleted", deleteDto);
            }

            var senderConnections = _connectionManager.GetConnectionsForUser(senderIdString);
            if (senderConnections.Any())
            {
                var senderUser = await _userRepository.GetByIdAsync(senderId);
                if (senderUser != null && senderUser.Role.IsGuest())
                    await _guestHubContext.Clients.Clients(senderConnections).SendAsync("MessageDeleted", deleteDto);
                else
                    await _hubContext.Clients.Clients(senderConnections).SendAsync("MessageDeleted", deleteDto);
            }
        }
    }

    public async Task NotifyMessagesRead(Guid userId, Guid senderId)
    {
        var readDto = new { UserId = userId, ReadAt = DateTime.UtcNow };

        var senderIdString = senderId.ToString();
        var senderConnections = _connectionManager.GetConnectionsForUser(senderIdString);
        if (senderConnections.Any())
        {
            var senderUser = await _userRepository.GetByIdAsync(senderId);
            if (senderUser != null && senderUser.Role.IsGuest())
                await _guestHubContext.Clients.Clients(senderConnections).SendAsync("MessagesRead", readDto);
            else
                await _hubContext.Clients.Clients(senderConnections).SendAsync("MessagesRead", readDto);
        }

        var userIdString = userId.ToString();
        var userConnections = _connectionManager.GetConnectionsForUser(userIdString);
        if (userConnections.Any())
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null && user.Role.IsGuest())
                await _guestHubContext.Clients.Clients(userConnections).SendAsync("MessagesRead", readDto);
            else
                await _hubContext.Clients.Clients(userConnections).SendAsync("MessagesRead", readDto);
        }
    }

    public async Task NotifyMessageReactionsUpdated(Guid messageId)
    {
        var payload = await _messageReadModelService.BuildMessageReactionsUpdatedPayloadAsync(messageId);
        if (payload is null) return;

        if (payload.BranchId.HasValue)
        {
            await _supportHubContext.Clients.Group("branch_" + payload.BranchId.Value).SendAsync("MessageReactionsUpdated", payload);
            if (payload.BranchMessageSenderId.HasValue)
                await _companyWidgetHubContext.Clients.Group("client_" + payload.BranchMessageSenderId.Value).SendAsync("MessageReactionsUpdated", payload);
            return;
        }

        if (payload.GroupId.HasValue)
        {
            await _hubContext.Clients.Group(payload.GroupId.Value.ToString())
                .SendAsync("MessageReactionsUpdated", payload);
        }
        else
        {
            foreach (var userId in payload.ParticipantUserIds)
            {
                var connections = _connectionManager.GetConnectionsForUser(userId.ToString());
                if (connections.Any())
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null && user.Role.IsGuest())
                        await _guestHubContext.Clients.Clients(connections).SendAsync("MessageReactionsUpdated", payload);
                    else
                        await _hubContext.Clients.Clients(connections).SendAsync("MessageReactionsUpdated", payload);
                }
            }
        }
    }

    public async Task NotifyMessagePinnedUpdated(Guid messageId)
    {
        var payload = await _messageReadModelService.BuildMessagePinnedUpdatedPayloadAsync(messageId);
        if (payload is null) return;

        if (payload.GroupId.HasValue)
        {
            await _hubContext.Clients.Group(payload.GroupId.Value.ToString())
                .SendAsync("MessagePinnedUpdated", payload);
        }
        else
        {
            foreach (var userId in payload.ParticipantUserIds)
            {
                var connections = _connectionManager.GetConnectionsForUser(userId.ToString());
                if (connections.Any())
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null && user.Role.IsGuest())
                        await _guestHubContext.Clients.Clients(connections).SendAsync("MessagePinnedUpdated", payload);
                    else
                        await _hubContext.Clients.Clients(connections).SendAsync("MessagePinnedUpdated", payload);
                }
            }
        }
    }
}
