using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly ApplicationDbContext _context;

    public OutboxRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.OutboxMessages
            .Where(o => o.Status == OutboxStatus.Pending && (o.NextAttemptAt == null || o.NextAttemptAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Update(message);
        return Task.CompletedTask;
    }
}
