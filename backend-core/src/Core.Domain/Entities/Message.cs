using System;
using Shared;
using Core.Domain.Events;

namespace Core.Domain.Entities;

public class Message : BaseEntity
{
    public Guid SenderId { get; private set; }
    public User Sender { get; private set; }
    /// <summary>When set, message is from a support user (SenderId is system user placeholder).</summary>
    public Guid? SupportSenderId { get; private set; }
    public SupportUser? SupportSender { get; private set; }
    /// <summary>When set, support reply is directed to this client (User) so only they receive it.</summary>
    public Guid? TargetReceiverUserId { get; private set; }
    public Guid? ReceiverId { get; private set; } // Nullable for group messages
    public User? Receiver { get; private set; }
    public Guid? GroupId { get; private set; } // Nullable for direct messages
    public Group? Group { get; private set; }
    public string Content { get; private set; }
    public string? AttachmentUrl { get; private set; }
    public string? AttachmentType { get; private set; }
    public DateTime SentAt { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? ReplyToMessageId { get; private set; }
    public Message? ReplyToMessage { get; private set; }
    public Guid? ReplyToStoryItemId { get; private set; }
    public StoryItem? ReplyToStoryItem { get; private set; }
    public Guid? ForwardedFromMessageId { get; private set; }
    public DateTime? ForwardedAt { get; private set; }
    public bool IsPinned { get; private set; }
    public DateTime? PinnedAt { get; private set; }
    public Guid? PinnedByUserId { get; private set; }
    /// <summary>Set for company widget messages; routes to branch inbox.</summary>
    public Guid? BranchId { get; private set; }
    /// <summary>Set for company widget messages.</summary>
    public Guid? CompanyId { get; private set; }
    /// <summary>Client-generated idempotency key. Unique per sender when set.</summary>
    public Guid? ClientMessageId { get; private set; }
    public string? SignatureDisplayName { get; private set; }
    public int ViewCount { get; private set; }
    public Guid? SendAsChannelId { get; private set; }
    public Group? SendAsChannel { get; private set; }
    public ICollection<MessageReaction> MessageReactions { get; private set; } = new List<MessageReaction>();
    public ICollection<MessageView> MessageViews { get; private set; } = new List<MessageView>();

    private Message() { } // EF Core

    public void AssignClientMessageId(Guid clientMessageId)
    {
        if (clientMessageId == Guid.Empty)
            throw new ArgumentException("ClientMessageId must be a non-empty GUID.", nameof(clientMessageId));
        if (ClientMessageId.HasValue && ClientMessageId.Value != clientMessageId)
            throw new InvalidOperationException("ClientMessageId is already assigned.");
        ClientMessageId = clientMessageId;
    }

    public Message(
        Guid senderId,
        Guid receiverId,
        string content,
        string? attachmentUrl = null,
        string? attachmentType = null,
        Guid? replyToMessageId = null,
        Guid? forwardedFromMessageId = null,
        DateTime? forwardedAt = null)
    {
        SenderId = senderId;
        ReceiverId = receiverId;
        Content = content;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        SentAt = DateTime.UtcNow;
        IsRead = false;
        IsEdited = false;
        DeletedAt = null;
        ReplyToMessageId = replyToMessageId;
        ForwardedFromMessageId = forwardedFromMessageId;
        ForwardedAt = forwardedAt;
        IsPinned = false;
        PinnedAt = null;
        PinnedByUserId = null;
        BranchId = null;
        CompanyId = null;

        AddDomainEvent(new MessageSentEvent(Id, SenderId, ReceiverId, null, Content, SentAt, AttachmentUrl, AttachmentType));
    }

    /// <summary>Constructor for company branch messages. ReceiverId is null; support users query by BranchId.</summary>
    public Message(
        Guid senderId,
        Guid companyId,
        Guid branchId,
        string content,
        string? attachmentUrl = null,
        string? attachmentType = null,
        Guid? replyToMessageId = null)
    {
        SenderId = senderId;
        ReceiverId = null;
        GroupId = null;
        CompanyId = companyId;
        BranchId = branchId;
        Content = content;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        SentAt = DateTime.UtcNow;
        IsRead = false;
        IsEdited = false;
        DeletedAt = null;
        ReplyToMessageId = replyToMessageId;
        ForwardedFromMessageId = null;
        ForwardedAt = null;
        IsPinned = false;
        PinnedAt = null;
        PinnedByUserId = null;

        AddDomainEvent(new MessageSentEvent(Id, senderId, null, null, Content, SentAt, AttachmentUrl, AttachmentType, branchId));
    }

    /// <summary>Constructor for support user reply. SenderId is systemSupportUserId (placeholder); SupportSenderId is the actual support user.</summary>
    public Message(
        Guid systemSupportUserId,
        Guid companyId,
        Guid branchId,
        Guid supportSenderId,
        Guid? targetReceiverUserId,
        string content,
        string? attachmentUrl = null,
        string? attachmentType = null,
        Guid? replyToMessageId = null)
    {
        SenderId = systemSupportUserId;
        SupportSenderId = supportSenderId;
        TargetReceiverUserId = targetReceiverUserId;
        ReceiverId = null;
        GroupId = null;
        CompanyId = companyId;
        BranchId = branchId;
        Content = content;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        SentAt = DateTime.UtcNow;
        IsRead = false;
        IsEdited = false;
        DeletedAt = null;
        ReplyToMessageId = replyToMessageId;
        ForwardedFromMessageId = null;
        ForwardedAt = null;
        IsPinned = false;
        PinnedAt = null;
        PinnedByUserId = null;

        AddDomainEvent(new MessageSentEvent(Id, systemSupportUserId, targetReceiverUserId, null, Content, SentAt, AttachmentUrl, AttachmentType, branchId, replyToMessageId, supportSenderId));
    }

    public Message(
        Guid senderId,
        Guid groupId,
        string content,
        bool isGroupMessage,
        string? attachmentUrl = null,
        string? attachmentType = null,
        Guid? replyToMessageId = null,
        Guid? forwardedFromMessageId = null,
        DateTime? forwardedAt = null)
    {
        SenderId = senderId;
        if (isGroupMessage)
        {
            GroupId = groupId;
            ReceiverId = null;
        }
        else
        {
            throw new ArgumentException("Use the other constructor for direct messages");
        }
        Content = content;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        SentAt = DateTime.UtcNow;
        IsRead = false;
        IsEdited = false;
        DeletedAt = null;
        ReplyToMessageId = replyToMessageId;
        ForwardedFromMessageId = forwardedFromMessageId;
        ForwardedAt = forwardedAt;
        IsPinned = false;
        PinnedAt = null;
        PinnedByUserId = null;
        BranchId = null;
        CompanyId = null;

        AddDomainEvent(new MessageSentEvent(Id, SenderId, null, GroupId, Content, SentAt, AttachmentUrl, AttachmentType));
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    public void Edit(string newContent)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot edit a deleted message.");

        Content = newContent;
        IsEdited = true;
        AddDomainEvent(new MessageEditedEvent(Id, SenderId, ReceiverId, GroupId, Content, SentAt));
    }

    public void Delete()
    {
        DeletedAt = DateTime.UtcNow;
        AddDomainEvent(new MessageDeletedEvent(Id, SenderId, ReceiverId, GroupId));
    }

    public void Pin(Guid userId)
    {
        if (DeletedAt.HasValue)
            throw new InvalidOperationException("Cannot pin a deleted message.");

        IsPinned = true;
        PinnedAt = DateTime.UtcNow;
        PinnedByUserId = userId;
    }

    public void Unpin()
    {
        IsPinned = false;
        PinnedAt = null;
        PinnedByUserId = null;
    }

    public void SetReplyToStoryItem(Guid storyItemId)
    {
        if (storyItemId == Guid.Empty)
            throw new ArgumentException("Story item id is required.", nameof(storyItemId));
        ReplyToStoryItemId = storyItemId;
    }

    public void SetSignature(string? displayName)
    {
        SignatureDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    public void SetSendAsChannel(Guid channelId)
    {
        if (channelId == Guid.Empty)
            throw new ArgumentException("Channel id is required.", nameof(channelId));
        SendAsChannelId = channelId;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
