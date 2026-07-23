using MediatR;

namespace Core.Application.Commands;

public class PinMessageCommand : IRequest
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
}

