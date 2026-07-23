using Shared;

namespace Core.Domain.Entities;

public class Story : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private readonly List<StoryItem> _items = new();
    public IReadOnlyCollection<StoryItem> Items => _items.AsReadOnly();

    private readonly List<StoryView> _views = new();
    public IReadOnlyCollection<StoryView> Views => _views.AsReadOnly();

    private Story() { }

    public Story(Guid userId, DateTime expiresAt)
    {
        UserId = userId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public StoryItem AddItem(string mediaType, string objectKey, int durationMs, string? textOverlayJson, int sortOrder)
    {
        var item = new StoryItem(Id, mediaType, objectKey, durationMs, textOverlayJson, sortOrder);
        _items.Add(item);
        return item;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsActive(DateTime utcNow) =>
        DeletedAt == null && ExpiresAt > utcNow;
}

public class StoryItem : BaseEntity
{
    public Guid StoryId { get; private set; }
    public Story Story { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public string MediaType { get; private set; } = "Image";
    public string ObjectKey { get; private set; } = string.Empty;
    public int DurationMs { get; private set; }
    public string? TextOverlayJson { get; private set; }

    private StoryItem() { }

    public StoryItem(Guid storyId, string mediaType, string objectKey, int durationMs, string? textOverlayJson, int sortOrder)
    {
        StoryId = storyId;
        MediaType = mediaType;
        ObjectKey = objectKey;
        DurationMs = durationMs;
        TextOverlayJson = textOverlayJson;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }
}

public class StoryView : BaseEntity
{
    public Guid StoryId { get; private set; }
    public Story Story { get; private set; } = null!;
    public Guid ViewerUserId { get; private set; }
    public User ViewerUser { get; private set; } = null!;
    public DateTime ViewedAt { get; private set; }

    private StoryView() { }

    public StoryView(Guid storyId, Guid viewerUserId)
    {
        StoryId = storyId;
        ViewerUserId = viewerUserId;
        ViewedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void Touch()
    {
        ViewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
