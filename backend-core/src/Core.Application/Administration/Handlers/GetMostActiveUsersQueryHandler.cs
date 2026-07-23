using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetMostActiveUsersQueryHandler : IRequestHandler<GetMostActiveUsersQuery, PaginatedResultDto<MostActiveUserDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetMostActiveUsersQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<MostActiveUserDto>> Handle(GetMostActiveUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetMostActiveUsersAsync(request.Page, request.PageSize, cancellationToken);
        return new PaginatedResultDto<MostActiveUserDto> { Items = items, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }
}
