using Shared;

namespace Core.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Message Message { get; private set; } = null!;
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }
    public Guid? SupportUserId { get; private set; }
    public SupportUser? SupportUser { get; private set; }
    public string Emoji { get; private set; } = string.Empty;

    private MessageReaction() { } // EF Core

    public MessageReaction(Guid messageId, Guid userId, string emoji)
    {
        MessageId = messageId;
        UserId = userId;
        SupportUserId = null;
        SetEmoji(emoji);
    }

    public MessageReaction(Guid messageId, Guid supportUserId, string emoji, bool fromSupport)
    {
        if (!fromSupport) throw new ArgumentException("Use the other constructor for user reactions.", nameof(fromSupport));
        MessageId = messageId;
        UserId = null;
        SupportUserId = supportUserId;
        SetEmoji(emoji);
    }

    public void SetEmoji(string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            throw new ArgumentException("Emoji is required.", nameof(emoji));

        Emoji = emoji;
        UpdatedAt = DateTime.UtcNow;
    }
}

