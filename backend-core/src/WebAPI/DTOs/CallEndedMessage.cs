namespace WebAPI.DTOs;

public class CallEndedMessage
{
    public string CallId { get; set; } = string.Empty;
    public string CallerId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public long? Duration { get; set; } // Stored as ticks
}
