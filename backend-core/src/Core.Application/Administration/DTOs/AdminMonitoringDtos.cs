namespace Core.Application.Administration.DTOs;

/// <summary>Chat message for admin viewer - optimized for bulk loading.</summary>
public class AdminChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUsername { get; set; } = "";
    public string? SenderFirstName { get; set; }
    public string? SenderLastName { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public string Content { get; set; } = "";
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsFromMe { get; set; } // For UI alignment - always false in admin view
}

/// <summary>Cursor-paginated conversation result.</summary>
public class AdminConversationResultDto
{
    public IEnumerable<AdminChatMessageDto> Messages { get; set; } = new List<AdminChatMessageDto>();
    public DateTime? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public string ConversationKey { get; set; } = "";
    public string ConversationTitle { get; set; } = "";
    public string Type { get; set; } = "";
}

/// <summary>Extended stats for monitoring dashboard.</summary>
public class ExtendedMonitoringStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalMessages { get; set; }
    public int TotalGroups { get; set; }
    public int OnlineUsersCount { get; set; }
    public int DisabledUsersCount { get; set; }
    public int SuspendedUsersCount { get; set; }
    public int UnreadMessagesCount { get; set; }
    public int NewUsersLast24h { get; set; }
    public int NewUsersLast7d { get; set; }
    public int NewUsersLast30d { get; set; }
    public int MessagesLast24h { get; set; }
    public int MessagesLast7d { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

/// <summary>Messages per day for trend chart.</summary>
public class MessagesPerDayDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

/// <summary>Top active user.</summary>
public class MostActiveUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public int MessageCount { get; set; }
}

/// <summary>Top active group.</summary>
public class MostActiveGroupDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public int MessageCount { get; set; }
}

/// <summary>DB table row counts.</summary>
public class TableRowCountsDto
{
    public Dictionary<string, long> Counts { get; set; } = new();
}

/// <summary>User storage/usage summary.</summary>
public class UserUsageDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public int MessageCount { get; set; }
    public int SavedMessagesCount { get; set; }
}
