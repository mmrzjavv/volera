using MediatR;

namespace Core.Application.Commands;

public class CreateGroupCommand : IRequest<Guid>
{
    public required string Name { get; set; }
    public Guid CreatorId { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
}
