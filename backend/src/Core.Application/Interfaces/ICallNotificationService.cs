namespace Core.Application.Interfaces;

public interface ICallNotificationService
{
    Task SendCallInitiated(string callId, Guid callerId, Guid receiverId);
    Task SendCallAccepted(string callId, Guid callerId, Guid receiverId);
    Task SendCallEnded(string callId, Guid callerId, Guid receiverId, TimeSpan? duration);
    Task SendMissedCall(string callId, Guid callerId, Guid receiverId);
}