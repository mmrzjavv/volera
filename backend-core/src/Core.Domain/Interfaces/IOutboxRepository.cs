using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default);
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
