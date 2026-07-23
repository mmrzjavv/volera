using MediatR;
using Core.Application.Administration.DTOs;
using Core.Application.Administration.Queries;
using Core.Domain.Interfaces;

namespace Core.Application.Administration.Handlers;

public class GetSystemLimitsQueryHandler : IRequestHandler<GetSystemLimitsQuery, IEnumerable<SystemLimitDto>>
{
    private readonly ISystemLimitRepository _systemLimitRepository;

    public GetSystemLimitsQueryHandler(ISystemLimitRepository systemLimitRepository)
    {
        _systemLimitRepository = systemLimitRepository;
    }

    public async Task<IEnumerable<SystemLimitDto>> Handle(GetSystemLimitsQuery request, CancellationToken cancellationToken)
    {
        var limits = await _systemLimitRepository.GetAllAsync(cancellationToken);
        return limits.Select(l => new SystemLimitDto { Key = l.Key, Value = l.Value, Description = l.Description });
    }
}
