using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetAdminChatListQueryHandler : IRequestHandler<GetAdminChatListQuery, PaginatedResultDto<AdminChatDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetAdminChatListQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<AdminChatDto>> Handle(GetAdminChatListQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.GetAdminChatListAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.TypeFilter,
            cancellationToken);
        return new PaginatedResultDto<AdminChatDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
