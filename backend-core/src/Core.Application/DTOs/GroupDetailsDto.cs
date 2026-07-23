namespace Core.Application.DTOs;

public class GroupDetailsDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid AdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? InviteCode { get; set; }
    public string Kind { get; set; } = "Group";
    public bool IsChannel { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
    public bool SignaturesEnabled { get; set; }
    public Guid? LinkedDiscussionGroupId { get; set; }
    public int SubscriberCount { get; set; }
    public bool CanPost { get; set; }
    public bool IsAdmin { get; set; }
    public bool CanManageSubscribers { get; set; }
    public bool CanChangeInfo { get; set; }
    public bool CanAddAdmins { get; set; }
    public bool CanEditMessages { get; set; }
    public bool CanDeleteMessages { get; set; }
    public List<UserDto> Members { get; set; } = new();
    public List<ChannelMemberDto> Admins { get; set; } = new();
}

public class ChannelMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public bool IsAdmin { get; set; }
    public bool CanPost { get; set; }
    public bool CanEditMessages { get; set; }
    public bool CanDeleteMessages { get; set; }
    public bool CanManageSubscribers { get; set; }
    public bool CanChangeInfo { get; set; }
    public bool CanAddAdmins { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class ChannelAnalyticsDto
{
    public Guid ChannelId { get; set; }
    public int SubscriberCount { get; set; }
    public int PostCount { get; set; }
    public long TotalViews { get; set; }
    public int PostsLast7Days { get; set; }
}

public class SuggestedPostDto
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? ScheduledAt { get; set; }
    public string? AdminNote { get; set; }
    public Guid? PublishedMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicChannelSearchResultDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? PublicUsername { get; set; }
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int SubscriberCount { get; set; }
}
