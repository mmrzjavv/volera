using Shared;

namespace Core.Domain.Entities;

public class GroupMember : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Group Group { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool CanPost { get; private set; }
    public bool CanEditMessages { get; private set; }
    public bool CanDeleteMessages { get; private set; }
    public bool CanManageSubscribers { get; private set; }
    public bool CanChangeInfo { get; private set; }
    public bool CanAddAdmins { get; private set; }

    private GroupMember() { }

    public GroupMember(Guid groupId, Guid userId, bool isAdmin = false)
    {
        GroupId = groupId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
        IsAdmin = isAdmin;
        if (isAdmin)
        {
            CanPost = true;
            CanEditMessages = true;
            CanDeleteMessages = true;
            CanManageSubscribers = true;
            CanChangeInfo = true;
            CanAddAdmins = true;
        }
    }

    public static GroupMember CreateChannelOwner(Guid channelId, Guid userId)
    {
        var m = new GroupMember(channelId, userId, true);
        m.GrantFullChannelAdminRights();
        return m;
    }

    public static GroupMember CreateChannelAdmin(Guid channelId, Guid userId)
    {
        var m = new GroupMember(channelId, userId, true);
        m.GrantFullChannelAdminRights();
        return m;
    }

    public static GroupMember CreateChannelSubscriber(Guid channelId, Guid userId)
    {
        return new GroupMember(channelId, userId, false);
    }

    public void PromoteToAdmin()
    {
        IsAdmin = true;
        CanPost = true;
        CanEditMessages = true;
        CanDeleteMessages = true;
        CanManageSubscribers = true;
        CanChangeInfo = true;
        CanAddAdmins = true;
    }

    public void DemoteFromAdmin()
    {
        IsAdmin = false;
        CanPost = false;
        CanEditMessages = false;
        CanDeleteMessages = false;
        CanManageSubscribers = false;
        CanChangeInfo = false;
        CanAddAdmins = false;
    }

    public void GrantFullChannelAdminRights()
    {
        IsAdmin = true;
        CanPost = true;
        CanEditMessages = true;
        CanDeleteMessages = true;
        CanManageSubscribers = true;
        CanChangeInfo = true;
        CanAddAdmins = true;
    }

    public void SetChannelAdminRights(
        bool canPost,
        bool canEditMessages,
        bool canDeleteMessages,
        bool canManageSubscribers,
        bool canChangeInfo,
        bool canAddAdmins)
    {
        IsAdmin = true;
        CanPost = canPost;
        CanEditMessages = canEditMessages;
        CanDeleteMessages = canDeleteMessages;
        CanManageSubscribers = canManageSubscribers;
        CanChangeInfo = canChangeInfo;
        CanAddAdmins = canAddAdmins;
    }

    public void RevokeChannelAdminRights()
    {
        DemoteFromAdmin();
    }
}
