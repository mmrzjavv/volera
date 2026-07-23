using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetExtendedMonitoringStatsQueryHandler : IRequestHandler<GetExtendedMonitoringStatsQuery, ExtendedMonitoringStatsDto>
{
    private readonly IAdminReadRepository _adminReadRepository;
    private readonly IOnlineUserService _onlineUserService;

    public GetExtendedMonitoringStatsQueryHandler(IAdminReadRepository adminReadRepository, IOnlineUserService onlineUserService)
    {
        _adminReadRepository = adminReadRepository;
        _onlineUserService = onlineUserService;
    }

    public async Task<ExtendedMonitoringStatsDto> Handle(GetExtendedMonitoringStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _adminReadRepository.GetExtendedMonitoringStatsAsync(cancellationToken);
        try
        {
            var onlineIds = await _onlineUserService.GetOnlineUserIds();
            stats.OnlineUsersCount = onlineIds?.Count() ?? 0;
        }
        catch { /* Redis may not be configured */ }
        return stats;
    }
}
