using System;
using Shared;

namespace Core.Domain.Entities;

public class SystemMessageRead : BaseEntity
{
    public Guid MessageId { get; private set; }
    public SystemMessage Message { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public DateTime ReadAt { get; private set; }

    private SystemMessageRead() { }

    public SystemMessageRead(Guid messageId, Guid userId)
    {
        MessageId = messageId;
        UserId = userId;
        ReadAt = DateTime.UtcNow;
    }
}
