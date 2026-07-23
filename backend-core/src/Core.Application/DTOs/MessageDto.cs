using System;

namespace Core.Application.DTOs;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public required string Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsEdited { get; set; }
    public bool IsSaved { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public ReplyToMessagePreviewDto? ReplyToMessagePreview { get; set; }
    public Guid? ReplyToStoryItemId { get; set; }
    public ReplyToStoryItemPreviewDto? ReplyToStoryItemPreview { get; set; }
    public List<MessageReactionDto> Reactions { get; set; } = new();
    public Guid? ForwardedFromMessageId { get; set; }
    public DateTime? ForwardedAt { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? PinnedAt { get; set; }
    public Guid? PinnedByUserId { get; set; }
    public Guid? ClientMessageId { get; set; }
    public string? SignatureDisplayName { get; set; }
    public int ViewCount { get; set; }
    public Guid? SendAsChannelId { get; set; }
    public string? SendAsChannelName { get; set; }
    public string? SendAsChannelProfilePictureUrl { get; set; }
}

public class SendMessageDto
{
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public required string Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public Guid? ClientMessageId { get; set; }
    public Guid? SendAsChannelId { get; set; }
}

public class SyncMessagesResultDto
{
    public List<MessageDto> Messages { get; set; } = new();
    public DateTime? NextAfterSentAt { get; set; }
    public Guid? NextAfterId { get; set; }
    public bool HasMore { get; set; }
}

public class ReplyToMessagePreviewDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public required string ContentSnippet { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
}

public class ReplyToStoryItemPreviewDto
{
    public Guid StoryItemId { get; set; }
    public Guid StoryOwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public string MediaType { get; set; } = "Image";
    public string? OverlaySnippet { get; set; }
}

public class MessageReactionDto
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? SupportUserId { get; set; }
    public string? SupportUserName { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
