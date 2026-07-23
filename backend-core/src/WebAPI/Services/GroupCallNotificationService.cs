using Core.Application.Interfaces;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class GroupCallNotificationService : IGroupCallNotificationService
{
    private readonly IHubContext<CallHub> _hubContext;
    private readonly IUserRepository _userRepository;
    private readonly IGroupCallRepository _groupCallRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IConnectionManager _connectionManager;
    private readonly IPushNotificationService _pushNotificationService;

    public GroupCallNotificationService(
        IHubContext<CallHub> hubContext,
        IUserRepository userRepository,
        IGroupCallRepository groupCallRepository,
        IGroupRepository groupRepository,
        IConnectionManager connectionManager,
        IPushNotificationService pushNotificationService)
    {
        _hubContext = hubContext;
        _userRepository = userRepository;
        _groupCallRepository = groupCallRepository;
        _groupRepository = groupRepository;
        _connectionManager = connectionManager;
        _pushNotificationService = pushNotificationService;
    }

    public async Task SendGroupCallInitiated(Guid groupCallId, Guid groupId, Guid initiatorId, string initiatorName, bool isVideo, IEnumerable<Guid> memberUserIds)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(groupId);
        var groupName = group?.Name ?? "Group";

        var payload = new
        {
            groupCallId = groupCallId.ToString(),
            groupId = groupId.ToString(),
            initiatorId = initiatorId.ToString(),
            initiatorName,
            isVideo
        };

        var userIds = memberUserIds.Select(id => id.ToString()).ToList();

        // Notify all group members (can be multiple devices per user)
        foreach (var userId in userIds)
        {
            var userGuid = Guid.Parse(userId);
            var connections = _connectionManager.GetConnectionsForUser(userId);
            if (connections.Any())
            {
                await _hubContext.Clients.Clients(connections).SendAsync("GroupCallInitiated", payload);
            }
            else
            {
                await _hubContext.Clients.User(userId).SendAsync("GroupCallInitiated", payload);
            }

            // Always send push for group call so user gets ringing when app is backgrounded/closed (e.g. on another device)
            if (userGuid != initiatorId)
            {
                try
                {
                    await _pushNotificationService.SendPushNotificationAsync(
                        userGuid,
                        $"Incoming call in {groupName}",
                        $"{initiatorName} started a {(isVideo ? "video" : "voice")} call",
                        new
                        {
                            type = "group_call_initiated",
                            groupCallId = groupCallId.ToString(),
                            groupId = groupId.ToString(),
                            groupName,
                            initiatorId = initiatorId.ToString(),
                            initiatorName,
                            isVideo
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GroupCallNotificationService] Push failed for user {userId}: {ex.Message}");
                }
            }
        }
    }

    public async Task SendParticipantJoined(Guid groupCallId, Guid userId, string userName)
    {
        var payload = new
        {
            groupCallId = groupCallId.ToString(),
            userId = userId.ToString(),
            userName
        };

        // Use the raw groupCallId as the SignalR group name to align with JoinCallGroup usage on the client.
        await _hubContext.Clients.Group(groupCallId.ToString()).SendAsync("GroupCallParticipantJoined", payload);

        // Additionally, make sure the original initiator always receives this notification,
        // even if their connection hasn't joined the SignalR group yet.
        var groupCall = await _groupCallRepository.GetByIdAsync(groupCallId);
        if (groupCall != null)
        {
            await _hubContext.Clients.User(groupCall.InitiatorId.ToString())
                .SendAsync("GroupCallParticipantJoined", payload);
        }
    }

    public async Task SendParticipantLeft(Guid groupCallId, Guid userId)
    {
        var payload = new
        {
            groupCallId = groupCallId.ToString(),
            userId = userId.ToString()
        };

        await _hubContext.Clients.Group(groupCallId.ToString()).SendAsync("GroupCallParticipantLeft", payload);
    }

    public async Task SendGroupCallEnded(Guid groupCallId)
    {
        var payload = new
        {
            groupCallId = groupCallId.ToString()
        };

        await _hubContext.Clients.Group(groupCallId.ToString()).SendAsync("GroupCallEnded", payload);
    }
}

