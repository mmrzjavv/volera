using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetUserUsageQueryHandler : IRequestHandler<GetUserUsageQuery, PaginatedResultDto<UserUsageDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetUserUsageQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<UserUsageDto>> Handle(GetUserUsageQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetUserUsageAsync(request.Page, request.PageSize, request.SortBy, request.SortDesc, cancellationToken);
        return new PaginatedResultDto<UserUsageDto> { Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }
}
