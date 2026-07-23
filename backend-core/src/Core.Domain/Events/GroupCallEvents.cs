namespace Core.Domain.Events;

public class GroupCallInitiatedEvent : IDomainEvent
{
    public Guid GroupCallId { get; }
    public Guid GroupId { get; }
    public Guid InitiatorId { get; }
    public bool IsVideo { get; }
    public DateTime OccurredOn { get; }

    public GroupCallInitiatedEvent(Guid groupCallId, Guid groupId, Guid initiatorId, bool isVideo)
    {
        GroupCallId = groupCallId;
        GroupId = groupId;
        InitiatorId = initiatorId;
        IsVideo = isVideo;
        OccurredOn = DateTime.UtcNow;
    }
}

public class GroupCallJoinedEvent : IDomainEvent
{
    public Guid GroupCallId { get; }
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public GroupCallJoinedEvent(Guid groupCallId, Guid userId)
    {
        GroupCallId = groupCallId;
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}

public class GroupCallLeftEvent : IDomainEvent
{
    public Guid GroupCallId { get; }
    public Guid UserId { get; }
    public DateTime OccurredOn { get; }

    public GroupCallLeftEvent(Guid groupCallId, Guid userId)
    {
        GroupCallId = groupCallId;
        UserId = userId;
        OccurredOn = DateTime.UtcNow;
    }
}

public class GroupCallEndedEvent : IDomainEvent
{
    public Guid GroupCallId { get; }
    public Guid GroupId { get; }
    public Guid EndedByUserId { get; }
    public DateTime OccurredOn { get; }

    public GroupCallEndedEvent(Guid groupCallId, Guid groupId, Guid endedByUserId)
    {
        GroupCallId = groupCallId;
        GroupId = groupId;
        EndedByUserId = endedByUserId;
        OccurredOn = DateTime.UtcNow;
    }
}

