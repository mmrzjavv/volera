using MediatR;

namespace Core.Application.Commands;

public class RemoveMemberCommand : IRequest
{
    public Guid GroupId { get; set; }
    public Guid AdminId { get; set; } // Admin performing the removal
    public Guid MemberId { get; set; } // Member being removed
}

