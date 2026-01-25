using Core.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;

namespace WebAPI.Services;

public class CallNotificationService : ICallNotificationService
{
    private readonly IHubContext<CallHub> _hubContext;

    public CallNotificationService(IHubContext<CallHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendCallInitiated(string callId, Guid callerId, Guid receiverId)
    {
        await _hubContext.Clients.Group(callId).SendAsync("CallInitiated", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }

    public async Task SendCallAccepted(string callId, Guid callerId, Guid receiverId)
    {
        await _hubContext.Clients.Group(callId).SendAsync("CallAccepted", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }

    public async Task SendCallEnded(string callId, Guid callerId, Guid receiverId, TimeSpan? duration)
    {
        await _hubContext.Clients.Group(callId).SendAsync("CallEnded", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId, Duration = duration });
    }

    public async Task SendMissedCall(string callId, Guid callerId, Guid receiverId)
    {
        await _hubContext.Clients.User(receiverId.ToString()).SendAsync("MissedCall", new { CallId = callId, CallerId = callerId, ReceiverId = receiverId });
    }
}