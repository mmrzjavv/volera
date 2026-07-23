using System;
using Shared;
using Core.Domain.Events;

namespace Core.Domain.Entities;

public class Call : BaseEntity
{
    public Guid CallerId { get; private set; }
    public User Caller { get; private set; }
    public Guid ReceiverId { get; private set; }
    public User Receiver { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public long? Duration { get; private set; } // Stored as ticks (long) instead of TimeSpan to support durations longer than 24 hours
    public CallStatus Status { get; private set; }
    public bool IsVideo { get; private set; }

    private Call() { } // EF Core

    public Call(Guid callerId, Guid receiverId, bool isVideo = false)
    {
        CallerId = callerId;
        ReceiverId = receiverId;
        StartTime = DateTime.UtcNow;
        Status = CallStatus.Ringing;
        IsVideo = isVideo;

        // Raise domain event
        AddDomainEvent(new CallInitiatedEvent(Id, CallerId, ReceiverId, IsVideo));
    }

    public void Accept()
    {
        // Idempotent: a second accept (double-click / retry) must not fail once Connected.
        // Re-raise the event so the caller can still start WebRTC if the first notify was missed.
        if (Status == CallStatus.Connected)
        {
            AddDomainEvent(new CallAcceptedEvent(Id, CallerId, ReceiverId));
            return;
        }

        if (Status != CallStatus.Ringing)
            throw new InvalidOperationException("Call can only be accepted if ringing.");

        Status = CallStatus.Connected;
        AddDomainEvent(new CallAcceptedEvent(Id, CallerId, ReceiverId));
    }

    public void Reject()
    {
        if (Status != CallStatus.Ringing)
            throw new InvalidOperationException("Call can only be rejected if ringing.");

        Status = CallStatus.Ended;
        EndTime = DateTime.UtcNow;
        Duration = (EndTime.Value - StartTime).Ticks;
        AddDomainEvent(new CallRejectedEvent(Id, CallerId, ReceiverId));
    }

    public void End()
    {
        if (Status == CallStatus.Ended)
            throw new InvalidOperationException("Call is already ended.");

        EndTime = DateTime.UtcNow;
        Duration = (EndTime.Value - StartTime).Ticks;
        Status = CallStatus.Ended;

        AddDomainEvent(new CallEndedEvent(Id, CallerId, ReceiverId, Duration));
    }

    public void MarkAsMissed()
    {
        if (Status != CallStatus.Ringing)
            return;

        Status = CallStatus.Missed;
        AddDomainEvent(new MissedCallEvent(Id, CallerId, ReceiverId));
    }

    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public enum CallStatus
{
    Ringing,
    Connected,
    Ended,
    Missed
}