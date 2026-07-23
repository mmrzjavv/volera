using Shared;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class SuggestedPost : BaseEntity
{
    public Guid ChannelId { get; private set; }
    public Group Channel { get; private set; } = null!;
    public Guid FromUserId { get; private set; }
    public User FromUser { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public string? AttachmentUrl { get; private set; }
    public string? AttachmentType { get; private set; }
    public SuggestedPostStatus Status { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public string? AdminNote { get; private set; }
    public Guid? PublishedMessageId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SuggestedPost() { }

    public SuggestedPost(Guid channelId, Guid fromUserId, string content, string? attachmentUrl = null, string? attachmentType = null)
    {
        ChannelId = channelId;
        FromUserId = fromUserId;
        Content = content;
        AttachmentUrl = attachmentUrl;
        AttachmentType = attachmentType;
        Status = SuggestedPostStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Accept(Guid publishedMessageId)
    {
        Status = SuggestedPostStatus.Accepted;
        PublishedMessageId = publishedMessageId;
        ScheduledAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? adminNote)
    {
        Status = SuggestedPostStatus.Rejected;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Schedule(DateTime scheduledAt, string? adminNote = null)
    {
        if (scheduledAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Scheduled time must be in the future.");
        Status = SuggestedPostStatus.Scheduled;
        ScheduledAt = scheduledAt;
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestEdit(string adminNote)
    {
        AdminNote = adminNote;
        UpdatedAt = DateTime.UtcNow;
    }
}
