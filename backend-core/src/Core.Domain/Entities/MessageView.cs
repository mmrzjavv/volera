using Shared;

namespace Core.Domain.Entities;

public class MessageView : BaseEntity
{
    public Guid MessageId { get; private set; }
    public Message Message { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public DateTime ViewedAt { get; private set; }

    private MessageView() { }

    public MessageView(Guid messageId, Guid userId)
    {
        MessageId = messageId;
        UserId = userId;
        ViewedAt = DateTime.UtcNow;
    }
}
