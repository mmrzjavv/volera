using MediatR;

namespace Core.Application.Commands;

public class SendMessageCommand : IRequest<Guid>
{
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public required string Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    /// <summary>When set, message is a reply to a story item (DM to story author).</summary>
    public Guid? ReplyToStoryItemId { get; set; }
    /// <summary>Client-generated idempotency key. Recommended for all new clients.</summary>
    public Guid? ClientMessageId { get; set; }
    /// <summary>When set, message is posted under a public channel identity (sender must be channel admin).</summary>
    public Guid? SendAsChannelId { get; set; }
}
