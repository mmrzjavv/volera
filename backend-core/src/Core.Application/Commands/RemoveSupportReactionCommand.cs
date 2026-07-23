using MediatR;

namespace Core.Application.Commands;

public class RemoveSupportReactionCommand : IRequest
{
    public Guid SupportUserId { get; set; }
    public Guid MessageId { get; set; }
}
