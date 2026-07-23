using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace Infrastructure.Services;

public class LimitResolutionService : ILimitResolutionService
{
    private readonly ISystemLimitRepository _systemLimitRepository;
    private readonly IUserLimitOverrideRepository _overrideRepository;

    public LimitResolutionService(
        ISystemLimitRepository systemLimitRepository,
        IUserLimitOverrideRepository overrideRepository)
    {
        _systemLimitRepository = systemLimitRepository;
        _overrideRepository = overrideRepository;
    }

    public async Task<decimal> GetEffectiveLimitAsync(Guid? userId, string limitKey, CancellationToken cancellationToken = default)
    {
        if (userId.HasValue)
        {
            var userOverride = await _overrideRepository.GetAsync(userId.Value, limitKey, cancellationToken);
            if (userOverride != null)
                return userOverride.Value;
        }
        var systemLimit = await _systemLimitRepository.GetByKeyAsync(limitKey, cancellationToken);
        return systemLimit?.Value ?? 0;
    }

    public async Task<IEnumerable<KeyValuePair<string, decimal>>> GetEffectiveLimitsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var systemLimits = await _systemLimitRepository.GetAllAsync(cancellationToken);
        var overrides = (await _overrideRepository.GetByUserIdAsync(userId, cancellationToken)).ToDictionary(o => o.LimitKey, o => o.Value);
        return systemLimits.Select(sl => new KeyValuePair<string, decimal>(sl.Key, overrides.GetValueOrDefault(sl.Key, sl.Value)));
    }
}
