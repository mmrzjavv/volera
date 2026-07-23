using Shared;

namespace Core.Domain.Entities;

/// <summary>
/// Represents a direct chat that the user has hidden from their recent chats list.
/// </summary>
public class HiddenChat : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public Guid OtherUserId { get; private set; }

    private HiddenChat() { }

    public HiddenChat(Guid userId, Guid otherUserId)
    {
        UserId = userId;
        OtherUserId = otherUserId;
    }
}
