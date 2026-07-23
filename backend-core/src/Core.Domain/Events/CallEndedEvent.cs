using Core.Domain.Events;

namespace Core.Domain.Events;

public class CallEndedEvent : IDomainEvent
{
    public Guid CallId { get; }
    public Guid CallerId { get; }
    public Guid ReceiverId { get; }
    public long? Duration { get; } // Stored as ticks
    public DateTime OccurredOn { get; }

    public CallEndedEvent(Guid callId, Guid callerId, Guid receiverId, long? duration)
    {
        CallId = callId;
        CallerId = callerId;
        ReceiverId = receiverId;
        Duration = duration;
        OccurredOn = DateTime.UtcNow;
    }
}