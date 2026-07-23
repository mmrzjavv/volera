namespace Core.Application.Interfaces;

public interface ILimitResolutionService
{
    Task<decimal> GetEffectiveLimitAsync(Guid? userId, string limitKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<KeyValuePair<string, decimal>>> GetEffectiveLimitsAsync(Guid userId, CancellationToken cancellationToken = default);
}
