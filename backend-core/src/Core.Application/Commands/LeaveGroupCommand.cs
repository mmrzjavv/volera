using MediatR;

namespace Core.Application.Commands;

public class LeaveGroupCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
}

