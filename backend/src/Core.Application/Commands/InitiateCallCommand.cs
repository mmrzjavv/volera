using MediatR;

namespace Core.Application.Commands;

public class InitiateCallCommand : IRequest<Guid>
{
    public Guid CallerId { get; set; }
    public Guid ReceiverId { get; set; }
}