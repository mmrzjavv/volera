using System;

namespace Core.Domain.Events;

public class MessageDeletedEvent : IDomainEvent
{
    public Guid MessageId { get; }
    public Guid SenderId { get; }
    public Guid? ReceiverId { get; }
    public Guid? GroupId { get; }
    public DateTime DeletedAt { get; }
    public DateTime OccurredOn => DeletedAt;

    public MessageDeletedEvent(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId)
    {
        MessageId = messageId;
        SenderId = senderId;
        ReceiverId = receiverId;
        GroupId = groupId;
        DeletedAt = DateTime.UtcNow;
    }
}
