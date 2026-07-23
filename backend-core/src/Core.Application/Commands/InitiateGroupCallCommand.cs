using MediatR;

namespace Core.Application.Commands;

public class InitiateGroupCallCommand : IRequest<Guid>
{
    public Guid GroupId { get; set; }
    public Guid InitiatorId { get; set; }
    public bool IsVideo { get; set; }
}

