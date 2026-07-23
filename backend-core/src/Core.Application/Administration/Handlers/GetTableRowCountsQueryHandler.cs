using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetTableRowCountsQueryHandler : IRequestHandler<GetTableRowCountsQuery, TableRowCountsDto>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetTableRowCountsQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<TableRowCountsDto> Handle(GetTableRowCountsQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetTableRowCountsAsync(cancellationToken);
    }
}
