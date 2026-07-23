using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IGroupRepository : IRepository<Group>
{
    Task<IEnumerable<Group>> GetGroupsForUserAsync(Guid userId);
    Task<IEnumerable<Group>> GetChannelsForUserAsync(Guid userId);
    Task<Group?> GetGroupWithMembersAsync(Guid groupId);
    Task<Group?> GetByInviteCodeAsync(string inviteCode);
    Task<Group?> GetByPublicUsernameAsync(string publicUsername);
    Task<IEnumerable<Group>> SearchPublicChannelsAsync(string query, int limit = 20);
    Task<int> GetSubscriberCountAsync(Guid channelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetGroupsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<bool> IsPublicUsernameTakenAsync(string publicUsername, Guid? excludeChannelId = null, CancellationToken cancellationToken = default);
    Task<Group?> GetChannelByLinkedDiscussionGroupIdAsync(Guid discussionGroupId, CancellationToken cancellationToken = default);
    void AddMember(GroupMember member);
}
