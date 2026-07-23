using MediatR;

namespace Core.Application.Commands;

public class AddSupportReactionCommand : IRequest
{
    public Guid SupportUserId { get; set; }
    public Guid MessageId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
