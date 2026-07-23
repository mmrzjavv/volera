using System;
using Shared;

namespace Core.Domain.Entities;

public class SystemMessage : BaseEntity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private SystemMessage() { }

    public SystemMessage(string title, string content, Guid authorId, DateTime? expiresAt)
    {
        Title = title;
        Content = content;
        AuthorId = authorId;
        ExpiresAt = expiresAt;
    }

    public void Update(string title, string content, DateTime? expiresAt)
    {
        Title = title;
        Content = content;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
