namespace Core.Application.Administration.DTOs;

public class AdminChatDto
{
    public string ConversationKey { get; set; } = "";
    public string Type { get; set; } = ""; // "Dm" or "Group"
    public Guid? UserId1 { get; set; }
    public Guid? UserId2 { get; set; }
    public string? UserName1 { get; set; }
    public string? UserName2 { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? GroupProfilePictureUrl { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
}
