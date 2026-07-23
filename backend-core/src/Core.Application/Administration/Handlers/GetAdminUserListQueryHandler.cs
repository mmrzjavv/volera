using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetAdminUserListQueryHandler : IRequestHandler<GetAdminUserListQuery, PaginatedResultDto<AdminUserListDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetAdminUserListQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<AdminUserListDto>> Handle(GetAdminUserListQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetAdminUserListAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.RoleFilter,
            request.IsDisabledFilter,
            request.SortBy,
            request.SortDesc,
            cancellationToken);
        return new PaginatedResultDto<AdminUserListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
