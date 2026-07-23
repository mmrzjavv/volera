namespace WebAPI.DTOs;

public class TestResponseMessage
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
}
