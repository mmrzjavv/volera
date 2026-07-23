using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

/// <summary>
/// Write and read operations for system-wide messages.
/// </summary>
public interface ISystemMessageRepository : IRepository<SystemMessage>
{
    /// <summary>
    /// Returns all active system messages (non-expired, active flag) ordered by creation time (newest first).
    /// </summary>
    Task<IReadOnlyList<SystemMessage>> GetActiveAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-tracking for system messages.
/// </summary>
public interface ISystemMessageReadRepository : IRepository<SystemMessageRead>
{
    /// <summary>
    /// Returns the set of message IDs that have been read by the given user.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetReadMessageIdsForUserAsync(Guid userId, IEnumerable<Guid> messageIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a read record already exists for the given message and user.
    /// </summary>
    Task<bool> HasReadAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);
}

