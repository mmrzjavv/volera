using Core.Domain.Events;

namespace Core.Domain.Events;

public class MissedCallEvent : IDomainEvent
{
    public Guid CallId { get; }
    public Guid CallerId { get; }
    public Guid ReceiverId { get; }
    public DateTime OccurredOn { get; }

    public MissedCallEvent(Guid callId, Guid callerId, Guid receiverId)
    {
        CallId = callId;
        CallerId = callerId;
        ReceiverId = receiverId;
        OccurredOn = DateTime.UtcNow;
    }
}