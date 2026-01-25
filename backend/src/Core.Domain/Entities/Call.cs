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
    public TimeSpan? Duration { get; private set; }
    public CallStatus Status { get; private set; }

    private Call() { } // EF Core

    public Call(Guid callerId, Guid receiverId)
    {
        CallerId = callerId;
        ReceiverId = receiverId;
        StartTime = DateTime.UtcNow;
        Status = CallStatus.Ringing;

        // Raise domain event
        AddDomainEvent(new CallInitiatedEvent(Id, CallerId, ReceiverId));
    }

    public void Accept()
    {
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
        Duration = EndTime - StartTime;
        AddDomainEvent(new CallRejectedEvent(Id, CallerId, ReceiverId));
    }

    public void End()
    {
        if (Status == CallStatus.Ended)
            throw new InvalidOperationException("Call is already ended.");

        EndTime = DateTime.UtcNow;
        Duration = EndTime - StartTime;
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