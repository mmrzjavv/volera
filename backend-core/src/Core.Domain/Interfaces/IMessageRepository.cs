using Shared;
using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetConversationAsync(Guid userId1, Guid userId2, int limit, DateTime? before);
    Task<IEnumerable<Message>> GetGroupMessagesAsync(Guid groupId, int limit, DateTime? before);
    Task<Message?> GetBySenderAndClientMessageIdAsync(Guid senderId, Guid clientMessageId, CancellationToken cancellationToken = default);
    /// <summary>Keyset sync: messages strictly after (afterSentAt, afterId) ordered ascending.</summary>
    Task<IEnumerable<Message>> SyncConversationAsync(Guid userId1, Guid userId2, DateTime? afterSentAt, Guid? afterId, int limit, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> SyncGroupMessagesAsync(Guid groupId, DateTime? afterSentAt, Guid? afterId, int limit, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetUnreadMessagesAsync(Guid userId);
    Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid userId);
    Task<IEnumerable<Core.Domain.Models.RecentChatResult>> GetRecentChatsAsync(Guid userId);
    Task<int> GetTotalCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid userId, Guid senderId);

    /// <summary>Delete all messages in a DM or group. Returns count deleted.</summary>
    Task<int> DeleteByConversationAsync(Guid? userId1, Guid? userId2, Guid? groupId, CancellationToken cancellationToken = default);

    /// <summary>Count messages sent by senderId since the given time (for rate limiting).</summary>
    Task<int> GetCountBySenderSinceAsync(Guid senderId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>Get messages for a branch (company widget inbox).</summary>
    Task<IEnumerable<Message>> GetByBranchIdAsync(Guid branchId, int limit, DateTime? before, CancellationToken cancellationToken = default);

    /// <summary>Get messages for a widget client: branch messages where client sent or is the target of a support reply.</summary>
    Task<IEnumerable<Message>> GetByBranchIdAndClientUserIdAsync(Guid branchId, Guid clientUserId, int limit, DateTime? before, CancellationToken cancellationToken = default);
}
