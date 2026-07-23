using Core.Domain.Events;

namespace Core.Domain.Events;

public class CallInitiatedEvent : IDomainEvent
{
    public Guid CallId { get; }
    public Guid CallerId { get; }
    public Guid ReceiverId { get; }
    public bool IsVideo { get; }
    public DateTime OccurredOn { get; }

    public CallInitiatedEvent(Guid callId, Guid callerId, Guid receiverId, bool isVideo)
    {
        CallId = callId;
        CallerId = callerId;
        ReceiverId = receiverId;
        IsVideo = isVideo;
        OccurredOn = DateTime.UtcNow;
    }
}