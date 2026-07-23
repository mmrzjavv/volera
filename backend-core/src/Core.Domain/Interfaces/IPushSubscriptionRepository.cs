using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<List<PushSubscription>> GetByUserIdAsync(Guid userId);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint);
    Task AddAsync(PushSubscription subscription);
    void Delete(PushSubscription subscription);
    Task DeleteByUserIdAsync(Guid userId);
    Task DeleteByEndpointAsync(Guid userId, string endpoint);
}
