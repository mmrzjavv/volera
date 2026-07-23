using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public PushSubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PushSubscription>> GetByUserIdAsync(Guid userId)
    {
        return await _context.PushSubscriptions
            .Where(ps => ps.UserId == userId)
            .ToListAsync();
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint)
    {
        return await _context.PushSubscriptions
            .FirstOrDefaultAsync(ps => ps.Endpoint == endpoint);
    }

    public async Task AddAsync(PushSubscription subscription)
    {
        await _context.PushSubscriptions.AddAsync(subscription);
    }

    public void Delete(PushSubscription subscription)
    {
        _context.PushSubscriptions.Remove(subscription);
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        var subscriptions = await _context.PushSubscriptions
            .Where(ps => ps.UserId == userId)
            .ToListAsync();
        
        if (subscriptions.Any())
        {
            _context.PushSubscriptions.RemoveRange(subscriptions);
        }
    }

    public async Task DeleteByEndpointAsync(Guid userId, string endpoint)
    {
        var subscription = await _context.PushSubscriptions
            .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Endpoint == endpoint);
            
        if (subscription != null)
        {
            _context.PushSubscriptions.Remove(subscription);
        }
    }
}
