namespace Core.Application.DTOs;

public class CallDto
{
    public Guid Id { get; set; }
    public Guid CallerId { get; set; }
    public required string CallerName { get; set; }
    public Guid ReceiverId { get; set; }
    public required string ReceiverName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long? Duration { get; set; } // Stored as ticks
    public required string Status { get; set; }
}