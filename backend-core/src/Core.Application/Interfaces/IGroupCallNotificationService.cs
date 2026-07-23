namespace Core.Application.Interfaces;

public interface IGroupCallNotificationService
{
    Task SendGroupCallInitiated(Guid groupCallId, Guid groupId, Guid initiatorId, string initiatorName, bool isVideo, IEnumerable<Guid> memberUserIds);
    Task SendParticipantJoined(Guid groupCallId, Guid userId, string userName);
    Task SendParticipantLeft(Guid groupCallId, Guid userId);
    Task SendGroupCallEnded(Guid groupCallId);
}

