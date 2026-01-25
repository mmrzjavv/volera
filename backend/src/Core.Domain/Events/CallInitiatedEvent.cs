using Core.Domain.Events;

namespace Core.Domain.Events;

public class CallInitiatedEvent : IDomainEvent
{
    public Guid CallId { get; }
    public Guid CallerId { get; }
    public Guid ReceiverId { get; }
    public DateTime OccurredOn { get; }

    public CallInitiatedEvent(Guid callId, Guid callerId, Guid receiverId)
    {
        CallId = callId;
        CallerId = callerId;
        ReceiverId = receiverId;
        OccurredOn = DateTime.UtcNow;
    }
}