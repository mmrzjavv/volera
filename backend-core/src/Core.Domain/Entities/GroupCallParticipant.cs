using Shared;

namespace Core.Domain.Entities;

public class GroupCallParticipant : BaseEntity
{
    public Guid GroupCallId { get; private set; }
    public GroupCall GroupCall { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    private GroupCallParticipant() { } // EF Core

    public GroupCallParticipant(Guid groupCallId, Guid userId)
    {
        GroupCallId = groupCallId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
    }

    public void MarkLeft(DateTime leftAt)
    {
        if (LeftAt != null)
            return;

        LeftAt = leftAt;
    }
}

