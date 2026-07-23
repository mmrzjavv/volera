using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.DTOs;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class SearchMessagesQueryHandler : IRequestHandler<SearchMessagesQuery, PaginatedResultDto<AdminMessageDto>>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public SearchMessagesQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<PaginatedResultDto<AdminMessageDto>> Handle(SearchMessagesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _adminReadRepository.SearchMessagesAsync(
            request.Page,
            request.PageSize,
            request.ContentSearch,
            request.SenderId,
            request.GroupId,
            request.DateFrom,
            request.DateTo,
            cancellationToken);
        return new PaginatedResultDto<AdminMessageDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
