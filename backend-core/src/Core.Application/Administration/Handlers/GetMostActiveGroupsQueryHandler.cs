using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetMostActiveGroupsQueryHandler : IRequestHandler<GetMostActiveGroupsQuery, PaginatedResultDto<MostActiveGroupDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetMostActiveGroupsQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<MostActiveGroupDto>> Handle(GetMostActiveGroupsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetMostActiveGroupsAsync(request.Page, request.PageSize, cancellationToken);
        return new PaginatedResultDto<MostActiveGroupDto> { Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }
}
