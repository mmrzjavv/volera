using System;

namespace Core.Domain.Events;

public class MessageSentEvent : IDomainEvent
{
    public Guid MessageId { get; }
    public Guid SenderId { get; }
    public Guid? ReceiverId { get; }
    public Guid? GroupId { get; }
    public string Content { get; }
    public DateTime SentAt { get; }
    public string? AttachmentUrl { get; }
    public string? AttachmentType { get; }
    /// <summary>Set for company branch messages; used to notify support hub and widget clients.</summary>
    public Guid? BranchId { get; }
    public Guid? ReplyToMessageId { get; }
    public Guid? SupportSenderId { get; }

    public DateTime OccurredOn => SentAt;

    public MessageSentEvent(Guid messageId, Guid senderId, Guid? receiverId, Guid? groupId, string content, DateTime sentAt, string? attachmentUrl = null, string? attachmentType = null, Guid? branchId = null, Guid? replyToMessageId = null, Guid? supportSenderId = null)
    {
        MessageId = messageId;
        SenderId = senderId;
        ReceiverId = receiverId;
        GroupId = groupId;
        Content = content;
        SentAt = sentAt;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        BranchId = branchId;
        ReplyToMessageId = replyToMessageId;
        SupportSenderId = supportSenderId;
    }
}
