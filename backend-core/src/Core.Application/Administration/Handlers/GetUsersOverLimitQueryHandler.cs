using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetUsersOverLimitQueryHandler : IRequestHandler<GetUsersOverLimitQuery, PaginatedResultDto<AdminUserListDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetUsersOverLimitQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<AdminUserListDto>> Handle(GetUsersOverLimitQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetUsersOverLimitAsync(request.LimitKey, request.Page, request.PageSize, cancellationToken);
        return new PaginatedResultDto<AdminUserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
