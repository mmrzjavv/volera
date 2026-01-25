using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Core.Domain.Events;

namespace WebAPI.Hubs;

[Authorize]
public class CallHub : Hub
{
    public async Task JoinCallGroup(string callId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, callId);
    }

    public async Task LeaveCallGroup(string callId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, callId);
    }

    // Methods to send events
    public async Task SendCallInitiated(string callId, Guid callerId, Guid receiverId)
    {
        await Clients.Group(callId).SendAsync("CallInitiated", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }

    public async Task SendCallAccepted(string callId, Guid callerId, Guid receiverId)
    {
        await Clients.Group(callId).SendAsync("CallAccepted", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }

    public async Task SendCallEnded(string callId, Guid callerId, Guid receiverId, TimeSpan? duration)
    {
        await Clients.Group(callId).SendAsync("CallEnded", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId, Duration = duration });
    }

    public async Task SendMissedCall(string callId, Guid callerId, Guid receiverId)
    {
        await Clients.User(receiverId.ToString()).SendAsync("MissedCall", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }

    // WebRTC Signaling methods
    public async Task SendOffer(string callId, string offer)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendAnswer(string callId, string answer)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(string callId, string candidate)
    {
        await Clients.OthersInGroup(callId).SendAsync("ReceiveIceCandidate", candidate);
    }
}