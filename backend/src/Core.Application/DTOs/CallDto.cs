namespace Core.Application.DTOs;

public class CallDto
{
    public Guid Id { get; set; }
    public Guid CallerId { get; set; }
    public string CallerName { get; set; }
    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; }
}