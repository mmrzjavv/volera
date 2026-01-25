using MediatR;

namespace Core.Application.Commands;

public class EndCallCommand : IRequest
{
    public Guid CallId { get; set; }
    public Guid UserId { get; set; }
}