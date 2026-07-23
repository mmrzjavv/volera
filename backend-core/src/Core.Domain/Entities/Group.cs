using Shared;
using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; private set; }
    public Guid AdminId { get; private set; }
    public User Admin { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? Description { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public string? InviteCode { get; private set; }
    public GroupKind Kind { get; private set; } = GroupKind.Group;
    public bool IsPublic { get; private set; }
    public string? PublicUsername { get; private set; }
    public bool SignaturesEnabled { get; private set; }
    public Guid? LinkedDiscussionGroupId { get; private set; }
    public Group? LinkedDiscussionGroup { get; private set; }

    private readonly List<GroupMember> _members = new();
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Group() { }

    public Group(string name, Guid adminId)
    {
        Name = name;
        AdminId = adminId;
        CreatedAt = DateTime.UtcNow;
        Kind = GroupKind.Group;
    }

    private Group(string name, Guid ownerId, string? description, bool isPublic, string? publicUsername)
    {
        Name = name;
        AdminId = ownerId;
        CreatedAt = DateTime.UtcNow;
        Kind = GroupKind.Channel;
        Description = description;
        IsPublic = isPublic;
        PublicUsername = NormalizeUsername(publicUsername);
    }

    public static Group CreateChannel(string name, Guid ownerId, string? description, bool isPublic, string? publicUsername)
    {
        var channel = new Group(name, ownerId, description, isPublic, publicUsername);
        channel.AddChannelOwner(ownerId);
        return channel;
    }

    public GroupMember? AddMember(Guid userId, bool isAdmin = false)
    {
        if (_members.Any(m => m.UserId == userId))
            return null;

        GroupMember member;
        if (Kind == GroupKind.Channel)
        {
            member = isAdmin
                ? GroupMember.CreateChannelAdmin(Id, userId)
                : GroupMember.CreateChannelSubscriber(Id, userId);
        }
        else
        {
            member = new GroupMember(Id, userId, isAdmin);
        }

        _members.Add(member);
        return member;
    }

    public GroupMember? AddChannelOwner(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            return null;

        var member = GroupMember.CreateChannelOwner(Id, userId);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
            _members.Remove(member);
    }

    public void ChangeAdmin(Guid newAdminUserId)
    {
        if (!_members.Any(m => m.UserId == newAdminUserId))
            throw new InvalidOperationException("New admin must be a member of the group.");

        AdminId = newAdminUserId;

        foreach (var member in _members)
        {
            if (member.UserId == newAdminUserId)
            {
                if (Kind == GroupKind.Channel)
                    member.GrantFullChannelAdminRights();
                else
                    member.PromoteToAdmin();
            }
            else if (member.IsAdmin && Kind != GroupKind.Channel)
            {
                member.DemoteFromAdmin();
            }
        }
    }

    public void UpdateProfile(string name, string? description, string? profilePictureUrl)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        Description = description;
        ProfilePictureUrl = profilePictureUrl;
    }

    public void SetVisibility(bool isPublic, string? publicUsername)
    {
        if (Kind != GroupKind.Channel)
            throw new InvalidOperationException("Only channels support public/private visibility.");

        IsPublic = isPublic;
        PublicUsername = isPublic ? NormalizeUsername(publicUsername) : null;
    }

    public void SetSignaturesEnabled(bool enabled)
    {
        if (Kind != GroupKind.Channel)
            throw new InvalidOperationException("Only channels support signatures.");
        SignaturesEnabled = enabled;
    }

    public void LinkDiscussionGroup(Guid discussionGroupId)
    {
        if (Kind != GroupKind.Channel)
            throw new InvalidOperationException("Only channels can link a discussion group.");
        if (discussionGroupId == Id)
            throw new InvalidOperationException("Cannot link a channel to itself.");
        LinkedDiscussionGroupId = discussionGroupId;
    }

    public void UnlinkDiscussionGroup()
    {
        LinkedDiscussionGroupId = null;
    }

    public void EnsureInviteCode()
    {
        if (!string.IsNullOrWhiteSpace(InviteCode))
            return;

        InviteCode = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("=", string.Empty)
            .Replace("+", string.Empty)
            .Replace("/", string.Empty);
    }

    public bool CanUserPost(Guid userId)
    {
        if (Kind != GroupKind.Channel)
            return _members.Any(m => m.UserId == userId);

        if (AdminId == userId)
            return true;

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        return member != null && member.CanPost;
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public GroupMember? GetMember(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId);

    private static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;
        return username.Trim().TrimStart('@').ToLowerInvariant();
    }
}
