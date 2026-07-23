using Core.Application.Interfaces;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebAPI.Hubs;
using WebAPI.DTOs;

namespace WebAPI.Services;

public class CallNotificationService : ICallNotificationService
{
    private readonly IHubContext<CallHub> _hubContext;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IUserRepository _userRepository;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<CallNotificationService> _logger;

    public CallNotificationService(
        IHubContext<CallHub> hubContext,
        IPushNotificationService pushNotificationService,
        IUserRepository userRepository,
        IConnectionManager connectionManager,
        ILogger<CallNotificationService> logger)
    {
        _hubContext = hubContext;
        _pushNotificationService = pushNotificationService;
        _userRepository = userRepository;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task SendCallInitiated(string callId, Guid callerId, Guid receiverId, bool isVideo)
    {
        // Send directly to the receiver, not to the group (receiver might not be in group yet)
        var receiverIdString = receiverId.ToString();
        var callerIdString = callerId.ToString();

        // Fetch caller info to get the name
        var caller = await _userRepository.GetByIdAsync(callerId);
        var callerName = caller != null ? $"{caller.FirstName} {caller.LastName}" : "Unknown";

        _logger.LogInformation(
            "Sending CallInitiated. CallId: {CallId}, CallerId: {CallerId}, CallerName: {CallerName}, ReceiverId: {ReceiverId}, IsVideo: {IsVideo}",
            callId, callerIdString, callerName, receiverIdString, isVideo);

        // Create consistent data object using DTO class (avoids JSON serialization issues)
        var callData = new CallInitiatedMessage
        {
            CallId = callId,
            CallerId = callerIdString,
            CallerName = callerName,
            ReceiverId = receiverIdString,
            IsVideo = isVideo
        };

        // PRIMARY METHOD: Send to all connections for this user using IConnectionManager
        var connectionsForUser = _connectionManager.GetConnectionsForUser(receiverIdString);

        if (connectionsForUser.Any())
        {
            _logger.LogInformation(
                "Found {ConnectionCount} connection(s) for user {ReceiverId}. ConnectionIds: {ConnectionIds}",
                connectionsForUser.Count, receiverIdString, string.Join(", ", connectionsForUser));
            try
            {
                await _hubContext.Clients.Clients(connectionsForUser).SendAsync("CallInitiated", callData);
                _logger.LogInformation("Sent CallInitiated via connection IDs for user {ReceiverId}.", receiverIdString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CallInitiated via connection IDs for user {ReceiverId}.", receiverIdString);
            }
        }
        else
        {
            _logger.LogWarning("No connections found for user {ReceiverId} when sending CallInitiated.", receiverIdString);
        }

        // FALLBACK: Clients.User (when IUserIdProvider maps userId claim)
        try
        {
            await _hubContext.Clients.User(receiverIdString).SendAsync("CallInitiated", callData);
            _logger.LogInformation("Also sent CallInitiated via Clients.User for user {ReceiverId}.", receiverIdString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CallInitiated via Clients.User for user {ReceiverId}.", receiverIdString);
        }

        // Do not broadcast to All — that leaks call metadata and floods every client.

        // Always send push notification so the receiver gets ringing on all devices (e.g. phone when app is closed, even if they have web open)
        try
        {
            await _pushNotificationService.SendPushNotificationAsync(
                receiverId,
                "Incoming Call",
                $"{callerName} is calling you",
                new
                {
                    callId,
                    callerId = callerIdString,
                    callerName,
                    receiverId = receiverIdString,
                    type = "call_initiated",
                    isVideo
                }
            );
            _logger.LogInformation("Push notification for CallInitiated sent to user {ReceiverId}. CallId: {CallId}", receiverIdString, callId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CallInitiated push notification to user {ReceiverId}. CallId: {CallId}", receiverIdString, callId);
        }

        _logger.LogInformation("Finished SendCallInitiated. CallId: {CallId}, CallerId: {CallerId}, ReceiverId: {ReceiverId}", callId, callerIdString, receiverIdString);
    }


    public async Task SendCallAccepted(string callId, Guid callerId, Guid receiverId)
    {
        var message = new CallAcceptedMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString()
        };

        // Caller must receive this even if JoinCallGroup raced or failed — otherwise no WebRTC offer is sent
        // and the callee stays on the Incoming UI (and a second Accept hits "only if ringing").
        var callerIdString = callerId.ToString();
        var callerConnections = _connectionManager.GetConnectionsForUser(callerIdString);
        if (callerConnections.Any())
        {
            try
            {
                await _hubContext.Clients.Clients(callerConnections).SendAsync("CallAccepted", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CallAccepted via connection IDs for caller {CallerId}.", callerIdString);
            }
        }

        try
        {
            await _hubContext.Clients.User(callerIdString).SendAsync("CallAccepted", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CallAccepted via Clients.User for caller {CallerId}.", callerIdString);
        }

        await _hubContext.Clients.Group(callId).SendAsync("CallAccepted", message);
    }

    public async Task SendCallRejected(string callId, Guid callerId, Guid receiverId)
    {
        var message = new CallRejectedMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString()
        };

        var callerIdString = callerId.ToString();
        var callerConnections = _connectionManager.GetConnectionsForUser(callerIdString);
        if (callerConnections.Any())
        {
            try
            {
                await _hubContext.Clients.Clients(callerConnections).SendAsync("CallRejected", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending CallRejected via connection IDs for caller {CallerId}.", callerIdString);
            }
        }

        try
        {
            await _hubContext.Clients.User(callerIdString).SendAsync("CallRejected", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending CallRejected via Clients.User for caller {CallerId}.", callerIdString);
        }

        await _hubContext.Clients.Group(callId).SendAsync("CallRejected", message);
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
        await _hubContext.Clients.Group(callId).SendAsync("CallEnded", message);
    }

    public async Task SendMissedCall(string callId, Guid callerId, Guid receiverId)
    {
        var message = new MissedCallMessage
        {
            CallId = callId,
            CallerId = callerId.ToString(),
            ReceiverId = receiverId.ToString()
        };
        await _hubContext.Clients.User(receiverId.ToString()).SendAsync("MissedCall", message);
    }
}