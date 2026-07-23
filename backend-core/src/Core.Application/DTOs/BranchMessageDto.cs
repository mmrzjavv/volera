using System;

namespace Core.Application.DTOs;

/// <summary>Sender (client or support) for a branch message. For widget clients, email and phone come from CompanyClient/Guest.</summary>
public class BranchMessageSenderDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
}

/// <summary>Support user who sent the message (when SupportSenderId is set).</summary>
public class BranchMessageSupportSenderDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}

/// <summary>Branch message for support inbox with enriched sender (name, email, phone) for widget clients.</summary>
public class BranchMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public BranchMessageSenderDto? Sender { get; set; }
    public Guid? SupportSenderId { get; set; }
    public BranchMessageSupportSenderDto? SupportSender { get; set; }
    public Guid? TargetReceiverUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public ReplyToMessagePreviewDto? ReplyToMessage { get; set; }
    public List<MessageReactionDto> MessageReactions { get; set; } = new();
}
