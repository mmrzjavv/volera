using System;
using Shared;

namespace Core.Domain.Entities;

public class SavedMessage : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public Guid MessageId { get; private set; }
    public Message Message { get; private set; }
    public DateTime SavedAt { get; private set; }

    private SavedMessage() { } // EF Core

    public SavedMessage(Guid userId, Guid messageId)
    {
        UserId = userId;
        MessageId = messageId;
        SavedAt = DateTime.UtcNow;
    }
}
