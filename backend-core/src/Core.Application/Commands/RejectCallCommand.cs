using MediatR;

namespace Core.Application.Commands;

public class RejectCallCommand : IRequest
{
    public Guid CallId { get; set; }
    public Guid UserId { get; set; }
}