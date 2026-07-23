using MediatR;

namespace Core.Application.Queries;

public class GetSupportUserBranchesQuery : IRequest<IEnumerable<BranchDto>>
{
    public Guid SupportUserId { get; set; }
}
