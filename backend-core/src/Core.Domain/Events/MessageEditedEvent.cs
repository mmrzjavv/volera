using System;

namespace Core.Domain.Events;

public class MessageEditedEvent : IDomainEvent
{
    public Guid MessageId { get; }
    public Guid SenderId { get; }
    public Guid? ReceiverId { get; }
    public Guid? GroupId { get; }
    public string NewContent { get; }
    public DateTime EditedAt { get; }
    public DateTime OccurredOn => EditedAt;

    public MessageEditedEvent(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string newContent, DateTime editedAt)
    {
        MessageId = messageId;
        SenderId = senderId;
        ReceiverId = receiverId;
        GroupId = groupId;
        NewContent = newContent;
        EditedAt = editedAt;
    }
}
