using System;

namespace Core.Application.DTOs;

public class RecentChatDto
{
    public Guid? UserId { get; set; }
    public Guid? GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public string? ProfilePicture { get; set; }
    public string LastMessageContent { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsGroup { get; set; }
    public bool IsChannel { get; set; }
    public string? PublicUsername { get; set; }
}