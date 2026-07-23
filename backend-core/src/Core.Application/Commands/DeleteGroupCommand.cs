using MediatR;

namespace Core.Application.Commands;

public class DeleteGroupCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid RequestingUserId { get; set; }
}
