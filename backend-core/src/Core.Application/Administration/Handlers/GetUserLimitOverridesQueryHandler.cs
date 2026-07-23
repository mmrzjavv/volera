using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetUserLimitOverridesQueryHandler : IRequestHandler<GetUserLimitOverridesQuery, IEnumerable<AdminLimitOverrideDto>>
{
    private readonly IUserLimitOverrideRepository _overrideRepository;

    public GetUserLimitOverridesQueryHandler(IUserLimitOverrideRepository overrideRepository)
    {
        _overrideRepository = overrideRepository;
    }

    public async Task<IEnumerable<AdminLimitOverrideDto>> Handle(GetUserLimitOverridesQuery request, CancellationToken cancellationToken)
    {
        var overrides = await _overrideRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return overrides.Select(o => new AdminLimitOverrideDto { LimitKey = o.LimitKey, Value = o.Value });
    }
}
