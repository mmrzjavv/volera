using Core.Application.Interfaces;
using Core.Application.Logging;
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
        var receiverIdString = receiverId.ToString();
        var callerIdString = callerId.ToString();

        var caller = await _userRepository.GetByIdAsync(callerId);
        var callerName = caller != null ? $"{caller.FirstName} {caller.LastName}" : "Unknown";

        var callData = new CallInitiatedMessage
        {
            CallId = callId,
            CallerId = callerIdString,
            CallerName = callerName,
            ReceiverId = receiverIdString,
            IsVideo = isVideo
        };

        var connectionsForUser = _connectionManager.GetConnectionsForUser(receiverIdString);
        var deliveredRealtime = false;

        if (connectionsForUser.Any())
        {
            try
            {
                await _hubContext.Clients.Clients(connectionsForUser).SendAsync("CallInitiated", callData);
                deliveredRealtime = true;
            }
            catch (Exception ex)
            {
                AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                    "CallId: {CallId} | ReceiverId: {ReceiverId} | Channel: ConnectionIds | Error: {ErrorType} | Result: Failure",
                    callId, receiverIdString, ex.GetType().Name);
            }
        }

        try
        {
            await _hubContext.Clients.User(receiverIdString).SendAsync("CallInitiated", callData);
            deliveredRealtime = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                "CallId: {CallId} | ReceiverId: {ReceiverId} | Channel: Clients.User | Error: {ErrorType} | Result: Failure",
                callId, receiverIdString, ex.GetType().Name);
        }

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
                });
        }
        catch (Exception ex)
        {
            AppLog.Error(_logger, AppLogEvents.PushFailed, ex,
                "UserId: {UserId} | CallId: {CallId} | Error: {ErrorType} | Result: Failure",
                receiverIdString, callId, ex.GetType().Name);
        }

        if (!deliveredRealtime)
        {
            AppLog.Warning(_logger, AppLogEvents.CallNotifyFailed,
                "CallId: {CallId} | CallerId: {CallerId} | ReceiverId: {ReceiverId} | Reason: NoActiveConnections | PushAttempted: true | Result: Partial",
                callId, callerIdString, receiverIdString);
        }
    }

    public async Task SendCallAccepted(string callId, Guid callerId, Guid receiverId)
    {
        var message = new CallAcceptedMessage
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
                await _hubContext.Clients.Clients(callerConnections).SendAsync("CallAccepted", message);
            }
            catch (Exception ex)
            {
                AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                    "CallId: {CallId} | CallerId: {CallerId} | Event: CallAccepted | Error: {ErrorType} | Result: Failure",
                    callId, callerIdString, ex.GetType().Name);
            }
        }

        try
        {
            await _hubContext.Clients.User(callerIdString).SendAsync("CallAccepted", message);
        }
        catch (Exception ex)
        {
            AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                "CallId: {CallId} | CallerId: {CallerId} | Event: CallAccepted | Channel: Clients.User | Error: {ErrorType} | Result: Failure",
                callId, callerIdString, ex.GetType().Name);
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
                AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                    "CallId: {CallId} | CallerId: {CallerId} | Event: CallRejected | Error: {ErrorType} | Result: Failure",
                    callId, callerIdString, ex.GetType().Name);
            }
        }

        try
        {
            await _hubContext.Clients.User(callerIdString).SendAsync("CallRejected", message);
        }
        catch (Exception ex)
        {
            AppLog.Error(_logger, AppLogEvents.CallNotifyFailed, ex,
                "CallId: {CallId} | CallerId: {CallerId} | Event: CallRejected | Channel: Clients.User | Error: {ErrorType} | Result: Failure",
                callId, callerIdString, ex.GetType().Name);
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
