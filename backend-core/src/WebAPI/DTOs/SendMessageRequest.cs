namespace WebAPI.DTOs;

public class SendMessageRequest
{
    public Guid? ReceiverId { get; set; }
    public Guid? GroupId { get; set; }
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public Guid? ClientMessageId { get; set; }
    public Guid? SendAsChannelId { get; set; }
}
