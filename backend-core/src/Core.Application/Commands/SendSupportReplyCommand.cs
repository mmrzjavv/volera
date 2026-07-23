using MediatR;

namespace Core.Application.Commands;

public class SendSupportReplyCommand : IRequest<Guid>
{
    public Guid SupportUserId { get; set; }
    public Guid BranchId { get; set; }
    /// <summary>Client (User) to receive the reply; when null, reply is visible to all in branch (e.g. broadcast).</summary>
    public Guid? TargetClientUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
