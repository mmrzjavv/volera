using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Domain.Events;
using Core.Application.Interfaces;
using WebAPI.DTOs;

namespace WebAPI.Hubs;

[Authorize]
public class CallHub : Hub
{
    private readonly IOnlineUserService _onlineUserService;
    private readonly IConnectionManager _connectionManager;

    public CallHub(IOnlineUserService onlineUserService, IConnectionManager connectionManager)
    {
        _onlineUserService = onlineUserService;
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        var nameIdentifier = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[CallHub] User connected - userId claim: {userIdClaim}, NameIdentifier: {nameIdentifier}, ConnectionId: {Context.ConnectionId}");

        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
        {
            // Register connection for notification (PRIMARY method for sending notifications)
            _connectionManager.RegisterConnection(Context.ConnectionId, userIdClaim);

            await _onlineUserService.UserConnected(userId);
            // Notify all other clients that this user is now online
            await Clients.Others.SendAsync("UserOnline", userId);
        }
        else
        {
            Console.WriteLine($"[CallHub] WARNING: Could not parse userId from claims. userIdClaim: {userIdClaim}");
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
            // Notify all other clients that this user is now offline
            await Clients.Others.SendAsync("UserOffline", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
    public async Task JoinCallGroup(string callId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
    }

    public async Task LeaveCallGroup(string callId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
    }

    // Methods to send events (using DTOs to avoid JSON serialization issues)
    public async Task SendCallInitiated(string callId, Guid callerId, Guid receiverId, bool isVideo)
    {
        var message = new CallInitiatedMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString(),
            IsVideo = isVideo
        };
        // Notify the receiver directly
        await Clients.User(receiverId.ToString()).SendAsync("CallInitiated", message);
    }

    public async Task SendCallAccepted(string callId, Guid callerId, Guid receiverId)
    {
        var message = new CallAcceptedMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString()
        };
        // Notify the caller directly
        await Clients.User(callerId.ToString()).SendAsync("CallAccepted", message);
        // Also notify group (e.g. other devices of the same user if any)
        await Clients.Group(callId).SendAsync("CallAccepted", message);
    }

    public async Task SendCallEnded(string callId, Guid callerId, Guid receiverId, long? duration)
    {
        var message = new CallEndedMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString(),
            Duration = duration
        };
        await Clients.Group(callId).SendAsync("CallEnded", message);
    }

    public async Task SendSignal(string callId, string data)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveSignal", data);
    }

    public async Task SendMissedCall(string callId, Guid callerId, Guid receiverId)
    {
        var message = new MissedCallMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString()
        };
        await Clients.User(receiverId.ToString()).SendAsync("MissedCall", message);
    }

    // WebRTC Signaling methods
    public async Task SendOffer(string callId, string offer)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendOfferToUser(string userId, string offer)
    {
        await Clients.User(userId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendAnswer(string callId, string answer)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(string callId, string candidate)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task SendIceCandidateToUser(string userId, string candidate)
    {
        await Clients.User(userId).SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task SendScreenShareStarted(string callId)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return;

        var message = new ScreenShareEventMessage { CallId = callId, UserId = userIdClaim };
        // Others only — sharer already updates local UI; including self breaks remote-screen state.
        await Clients.OthersInGroup(callId).SendAsync("ScreenShareStarted", message);
    }

    public async Task SendScreenShareStopped(string callId)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return;

        var message = new ScreenShareEventMessage { CallId = callId, UserId = userIdClaim };
        await Clients.OthersInGroup(callId).SendAsync("ScreenShareStopped", message);
    }

    public async Task SendScreenShareAudioEnabled(string callId, bool enabled)
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return;

        await Clients.OthersInGroup(callId).SendAsync("ScreenShareAudioEnabled", new { userId = userIdClaim, enabled });
    }

    // Test method to verify connection
    public async Task TestConnection()
    {
        var userIdClaim = Context.User?.FindFirst("userId")?.Value;
        var response = new TestResponseMessage
        {
            Message = "Connection test successful",
            UserId = userIdClaim,
            ConnectionId = Context.ConnectionId
        };
        await Clients.Caller.SendAsync("TestResponse", response);
    }
}