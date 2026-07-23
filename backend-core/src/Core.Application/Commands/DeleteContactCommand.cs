using MediatR;

namespace Core.Application.Commands;

public class DeleteContactCommand : IRequest
{
    public Guid ContactId { get; set; }
    public Guid UserId { get; set; }
}
