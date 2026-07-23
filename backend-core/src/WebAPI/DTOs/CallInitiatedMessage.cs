namespace WebAPI.DTOs;

public class CallInitiatedMessage
{
    public string CallId { get; set; } = string.Empty;
    public string CallerId { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public bool IsVideo { get; set; }
}
