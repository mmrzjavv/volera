namespace Core.Application.DTOs;

public class CreateStoryItemDto
{
    public required string ObjectKey { get; set; }
    public required string MediaType { get; set; }
    public int? DurationMs { get; set; }
    public string? TextOverlayJson { get; set; }
}

public class CreateStoryRequestDto
{
    public List<CreateStoryItemDto> Items { get; set; } = new();
}

public class StoryItemDto
{
    public Guid Id { get; set; }
    public string MediaType { get; set; } = "Image";
    public string ObjectKey { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public int DurationMs { get; set; }
    public string? TextOverlayJson { get; set; }
    public int SortOrder { get; set; }
}

public class StoryDto
{
    public Guid StoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool ViewedByMe { get; set; }
    public List<StoryItemDto> Items { get; set; } = new();
}

public class StoryRingDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public bool HasUnseen { get; set; }
    public bool IsOwn { get; set; }
    public DateTime LatestCreatedAt { get; set; }
    public List<StoryDto> Stories { get; set; } = new();
}

public class StoryViewerDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public DateTime ViewedAt { get; set; }
}

public class ReplyToStoryRequestDto
{
    public required string Content { get; set; }
}
