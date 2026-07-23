using MediatR;
using Core.Application.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyBranchesQueryHandler : IRequestHandler<GetCompanyBranchesQuery, IEnumerable<BranchDto>>
{
    private readonly IBranchRepository _branchRepository;

    public GetCompanyBranchesQueryHandler(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<IEnumerable<BranchDto>> Handle(GetCompanyBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await _branchRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        return branches.Select(b => new BranchDto
        {
            Id = b.Id,
            CompanyId = b.CompanyId,
            Name = b.Name,
            Address = b.Address,
            PhoneNumber = b.PhoneNumber,
            Email = b.Email,
            IsActive = b.IsActive
        });
    }
}
