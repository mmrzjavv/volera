using MediatR;

namespace Core.Application.Commands;

public class SendCompanyMessageCommand : IRequest<Guid>
{
    public string ClientToken { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
