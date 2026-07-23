using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Application.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetEffectiveLimitsQueryHandler : IRequestHandler<GetEffectiveLimitsQuery, IEnumerable<AdminLimitOverrideDto>>
{
    private readonly ILimitResolutionService _limitResolutionService;

    public GetEffectiveLimitsQueryHandler(ILimitResolutionService limitResolutionService)
    {
        _limitResolutionService = limitResolutionService;
    }

    public async Task<IEnumerable<AdminLimitOverrideDto>> Handle(GetEffectiveLimitsQuery request, CancellationToken cancellationToken)
    {
        var limits = await _limitResolutionService.GetEffectiveLimitsAsync(request.UserId, cancellationToken);
        return limits.Select(kv => new AdminLimitOverrideDto { LimitKey = kv.Key, Value = kv.Value });
    }
}
