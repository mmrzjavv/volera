using Core.Domain.Events;
using Shared;

namespace Core.Domain.Entities;

public class GroupCall : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Group Group { get; private set; } = null!;

    public Guid InitiatorId { get; private set; }
    public User Initiator { get; private set; } = null!;

    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public bool IsVideo { get; private set; }
    public GroupCallStatus Status { get; private set; }

    private readonly List<GroupCallParticipant> _participants = new();
    public IReadOnlyCollection<GroupCallParticipant> Participants => _participants.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private GroupCall() { } // EF Core

    public GroupCall(Guid groupId, Guid initiatorId, bool isVideo = false)
    {
        GroupId = groupId;
        InitiatorId = initiatorId;
        IsVideo = isVideo;
        StartTime = DateTime.UtcNow;
        Status = GroupCallStatus.Ringing;

        AddParticipant(initiatorId);

        AddDomainEvent(new GroupCallInitiatedEvent(Id, GroupId, InitiatorId, IsVideo));
    }

    public void Start()
    {
        if (Status != GroupCallStatus.Ringing)
            throw new InvalidOperationException("Group call can only be started when ringing.");

        Status = GroupCallStatus.Active;
    }

    public void End(Guid endedByUserId)
    {
        if (Status == GroupCallStatus.Ended)
            return;

        Status = GroupCallStatus.Ended;
        EndTime = DateTime.UtcNow;

        foreach (var participant in _participants.Where(p => p.LeftAt == null))
        {
            participant.MarkLeft(EndTime.Value);
        }

        AddDomainEvent(new GroupCallEndedEvent(Id, GroupId, endedByUserId));
    }

    public GroupCallParticipant AddParticipant(Guid userId)
    {
        if (_participants.Any(p => p.UserId == userId && p.LeftAt == null))
        {
            return _participants.First(p => p.UserId == userId && p.LeftAt == null);
        }

        var participant = new GroupCallParticipant(Id, userId);
        _participants.Add(participant);

        AddDomainEvent(new GroupCallJoinedEvent(Id, userId));

        return participant;
    }

    public void RemoveParticipant(Guid userId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (participant == null)
            return;

        participant.MarkLeft(DateTime.UtcNow);
        AddDomainEvent(new GroupCallLeftEvent(Id, userId));

        // If last active participant leaves, end the call
        if (_participants.All(p => p.LeftAt != null))
        {
            End(userId);
        }
    }

    private void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

