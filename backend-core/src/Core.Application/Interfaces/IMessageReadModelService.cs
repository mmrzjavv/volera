using Core.Application.DTOs;

namespace Core.Application.Interfaces;

/// <summary>
/// Application-level read model service for messages, responsible for assembling
/// DTOs and notification payloads from the underlying persistence store.
/// This keeps EF Core and data access concerns out of the WebAPI layer.
/// </summary>
public interface IMessageReadModelService
{
    /// <summary>
    /// Builds a complete <see cref="MessageDto"/> for real-time notifications, including
    /// reply preview data when available. Falls back to the provided minimal data when
    /// the message has not yet been fully persisted or cannot be found.
    /// </summary>
    Task<MessageDto> BuildMessageDtoForNotificationAsync(
        Guid messageId,
        Guid senderId,
        Guid? receiverId,
        Guid? groupId,
        string content,
        DateTime sentAt,
        string? attachmentUrl,
        string? attachmentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the payload for a "message reactions updated" real-time notification.
    /// Returns null if the message no longer exists.
    /// </summary>
    Task<MessageReactionsUpdatedPayload?> BuildMessageReactionsUpdatedPayloadAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the payload for a "message pinned updated" real-time notification.
    /// Returns null if the message no longer exists.
    /// </summary>
    Task<MessagePinnedUpdatedPayload?> BuildMessagePinnedUpdatedPayloadAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}

public sealed record MessageReactionsUpdatedPayload(
    Guid MessageId,
    Guid? GroupId,
    Guid? BranchId,
    Guid? BranchMessageSenderId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    IReadOnlyCollection<MessageReactionDto> Reactions);

public sealed record MessagePinnedUpdatedPayload(
    Guid MessageId,
    Guid? GroupId,
    IReadOnlyCollection<Guid> ParticipantUserIds,
    bool IsPinned,
    DateTime? PinnedAt,
    Guid? PinnedByUserId);

