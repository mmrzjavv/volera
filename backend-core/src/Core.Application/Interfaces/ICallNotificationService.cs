namespace Core.Application.Interfaces;

public interface ICallNotificationService
{
    Task SendCallInitiated(string callId, Guid callerId, Guid receiverId, bool isVideo);
    Task SendCallAccepted(string callId, Guid callerId, Guid receiverId);
    Task SendCallRejected(string callId, Guid callerId, Guid receiverId);
    Task SendCallEnded(string callId, Guid callerId, Guid receiverId, long? duration); // Duration in ticks
    Task SendMissedCall(string callId, Guid callerId, Guid receiverId);
}