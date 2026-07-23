using MediatR;

namespace Core.Application.Commands;

public class SendGuestMessageCommand : IRequest<Guid>
{
    public string GuestToken { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
}
