using MediatR;

namespace Core.Application.Commands;

public class CreateChannelCommand : IRequest<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
    public Guid CreatorId { get; set; }
}

public class SubscribeToChannelCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
}

public class LeaveChannelCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid UserId { get; set; }
}

public class JoinChannelByInviteCommand : IRequest<Guid>
{
    public required string InviteCode { get; set; }
    public Guid UserId { get; set; }
}

public class GenerateChannelInviteLinkCommand : IRequest<string>
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
}

public class UpdateChannelProfileCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class SetChannelVisibilityCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicUsername { get; set; }
}

public class SetChannelAdminCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public bool CanPost { get; set; } = true;
    public bool CanEditMessages { get; set; } = true;
    public bool CanDeleteMessages { get; set; } = true;
    public bool CanManageSubscribers { get; set; } = true;
    public bool CanChangeInfo { get; set; } = true;
    public bool CanAddAdmins { get; set; }
}

public class RemoveChannelAdminCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public Guid TargetUserId { get; set; }
}

public class TransferChannelOwnershipCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid CurrentOwnerId { get; set; }
    public Guid NewOwnerId { get; set; }
}

public class AddChannelSubscriberCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public Guid UserId { get; set; }
}

public class RemoveChannelSubscriberCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public Guid UserId { get; set; }
}

public class ToggleChannelSignaturesCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public bool Enabled { get; set; }
}

public class RecordChannelMessageViewsCommand : IRequest<int>
{
    public Guid UserId { get; set; }
    public List<Guid> MessageIds { get; set; } = new();
}

public class LinkDiscussionGroupCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid DiscussionGroupId { get; set; }
    public Guid RequestingUserId { get; set; }
}

public class UnlinkDiscussionGroupCommand : IRequest
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
}

public class SuggestChannelPostCommand : IRequest<Guid>
{
    public Guid ChannelId { get; set; }
    public Guid FromUserId { get; set; }
    public required string Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
}

public class AcceptSuggestedPostCommand : IRequest<Guid>
{
    public Guid SuggestedPostId { get; set; }
    public Guid RequestingUserId { get; set; }
}

public class RejectSuggestedPostCommand : IRequest
{
    public Guid SuggestedPostId { get; set; }
    public Guid RequestingUserId { get; set; }
    public string? AdminNote { get; set; }
}

public class ScheduleSuggestedPostCommand : IRequest
{
    public Guid SuggestedPostId { get; set; }
    public Guid RequestingUserId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? AdminNote { get; set; }
}
