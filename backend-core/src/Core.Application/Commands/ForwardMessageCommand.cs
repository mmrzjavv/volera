using MediatR;

namespace Core.Application.Commands;

public class ForwardMessageCommand : IRequest<Guid>
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
}

