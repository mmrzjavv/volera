using Shared;

namespace Core.Domain.Entities;

public enum OutboxStatus
{
    Pending = 0,
    Processed = 1,
    DeadLetter = 2
}

/// <summary>Transactional outbox row written in the same DB commit as the business entity.</summary>
public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
        Status = OutboxStatus.Pending;
        NextAttemptAt = DateTime.UtcNow;
    }

    public void MarkProcessed()
    {
        Status = OutboxStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkRetry(string error, TimeSpan delay, int maxAttempts)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
        if (AttemptCount >= maxAttempts)
        {
            Status = OutboxStatus.DeadLetter;
            NextAttemptAt = null;
        }
        else
        {
            Status = OutboxStatus.Pending;
            NextAttemptAt = DateTime.UtcNow.Add(delay);
        }
    }
}
