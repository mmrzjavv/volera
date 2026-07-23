namespace WebAPI.DTOs;

public class MissedCallMessage
{
    public string CallId { get; set; } = string.Empty;
    public string CallerId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
}
