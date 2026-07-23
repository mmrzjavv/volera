using MediatR;

namespace Core.Application.Commands;

public class AddMemberCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid AdminId { get; set; } // Only admins can add members
    public Guid MemberId { get; set; }
}
