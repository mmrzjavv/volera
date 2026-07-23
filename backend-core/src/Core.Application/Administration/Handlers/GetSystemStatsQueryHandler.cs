using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetSystemStatsQueryHandler : IRequestHandler<GetSystemStatsQuery, SystemStatsDto>
{
    private readonly IAdminReadRepository _adminReadRepository;

    public GetSystemStatsQueryHandler(IAdminReadRepository adminReadRepository)
    {
        _adminReadRepository = adminReadRepository;
    }

    public async Task<SystemStatsDto> Handle(GetSystemStatsQuery request, CancellationToken cancellationToken)
    {
        return await _adminReadRepository.GetSystemStatsAsync(cancellationToken);
    }
}
