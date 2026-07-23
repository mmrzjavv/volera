namespace Core.Application.Administration.DTOs;

public class AdminMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public string Content { get; set; } = "";
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? SenderUsername { get; set; }
}
