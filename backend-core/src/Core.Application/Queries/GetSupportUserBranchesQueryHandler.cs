using MediatR;
using Core.Application.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetSupportUserBranchesQueryHandler : IRequestHandler<GetSupportUserBranchesQuery, IEnumerable<BranchDto>>
{
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;

    public GetSupportUserBranchesQueryHandler(ISupportUserBranchRepository supportUserBranchRepository)
    {
        _supportUserBranchRepository = supportUserBranchRepository;
    }

    public async Task<IEnumerable<BranchDto>> Handle(GetSupportUserBranchesQuery request, CancellationToken cancellationToken)
    {
        var assignments = await _supportUserBranchRepository.GetBySupportUserIdAsync(request.SupportUserId, cancellationToken);
        return assignments.Select(a => new BranchDto
        {
            Id = a.Branch.Id,
            CompanyId = a.Branch.CompanyId,
            Name = a.Branch.Name,
            Address = a.Branch.Address,
            PhoneNumber = a.Branch.PhoneNumber,
            Email = a.Branch.Email,
            IsActive = a.Branch.IsActive
        });
    }
}
