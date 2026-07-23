using MediatR;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetCompanyContentQueryHandler : IRequestHandler<GetCompanyContentQuery, IReadOnlyList<CompanyContentBlockDto>>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IAiContentBlockRepository _contentBlockRepository;

    public GetCompanyContentQueryHandler(
        IBranchRepository branchRepository,
        IAiContentBlockRepository contentBlockRepository)
    {
        _branchRepository = branchRepository;
        _contentBlockRepository = contentBlockRepository;
    }

    public async Task<IReadOnlyList<CompanyContentBlockDto>> Handle(GetCompanyContentQuery request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository.GetByIdAsync(request.BranchId);
        if (branch == null || branch.CompanyId != request.CompanyId)
            return Array.Empty<CompanyContentBlockDto>();

        var blocks = await _contentBlockRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        return blocks.Select(b => new CompanyContentBlockDto
        {
            Id = b.Id,
            ContentSnippet = b.ContentSnippet,
            Status = b.Status.ToString(),
            JobId = b.JobId,
            ErrorMessage = b.ErrorMessage,
            CreatedAt = b.CreatedAt
        }).ToList();
    }
}
