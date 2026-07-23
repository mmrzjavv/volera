using MediatR;

namespace Core.Application.Commands;

public class GenerateGroupInviteLinkCommand : IRequest<string>
{
    public Guid GroupId { get; set; }
    public Guid RequestingUserId { get; set; }
}

