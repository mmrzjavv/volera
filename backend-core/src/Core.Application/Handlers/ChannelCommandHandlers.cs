using Core.Application.Commands;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateChannelCommandHandler(IGroupRepository groupRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var creator = await _userRepository.GetByIdAsync(request.CreatorId)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.IsPublic)
        {
            if (string.IsNullOrWhiteSpace(request.PublicUsername))
                throw new InvalidOperationException("Public channels require a username.");
            if (await _groupRepository.IsPublicUsernameTakenAsync(request.PublicUsername, null, cancellationToken))
                throw new InvalidOperationException("Channel username is already taken.");
            var existingUser = await _userRepository.GetByUsernameAsync(request.PublicUsername.Trim().TrimStart('@'));
            if (existingUser != null)
                throw new InvalidOperationException("Channel username conflicts with an existing user.");
        }

        var channel = Group.CreateChannel(request.Name, creator.Id, request.Description, request.IsPublic, request.PublicUsername);
        channel.EnsureInviteCode();
        await _groupRepository.AddAsync(channel);
        await _unitOfWork.SaveChangesAsync();
        return channel.Id;
    }
}

public class SubscribeToChannelCommandHandler : IRequestHandler<SubscribeToChannelCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscribeToChannelCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SubscribeToChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (!channel.IsPublic && !channel.IsMember(request.UserId))
            throw new UnauthorizedAccessException("Private channel requires an invite.");

        channel.AddMember(request.UserId, false);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class LeaveChannelCommandHandler : IRequestHandler<LeaveChannelCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveChannelCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LeaveChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (channel.AdminId == request.UserId)
            throw new InvalidOperationException("Owner cannot leave; transfer ownership first or delete the channel.");

        channel.RemoveMember(request.UserId);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class JoinChannelByInviteCommandHandler : IRequestHandler<JoinChannelByInviteCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JoinChannelByInviteCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(JoinChannelByInviteCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetByInviteCodeAsync(request.InviteCode)
            ?? throw new KeyNotFoundException("Invite not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Invite is not for a channel.");

        channel.AddMember(request.UserId, false);
        await _unitOfWork.SaveChangesAsync();
        return channel.Id;
    }
}

public class GenerateChannelInviteLinkCommandHandler : IRequestHandler<GenerateChannelInviteLinkCommand, string>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateChannelInviteLinkCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(GenerateChannelInviteLinkCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        EnsureChannelAdmin(channel, request.RequestingUserId, requireManageSubscribers: true);
        channel.EnsureInviteCode();
        await _unitOfWork.SaveChangesAsync();
        return channel.InviteCode!;
    }

    internal static void EnsureChannelAdmin(Group channel, Guid userId, bool requireManageSubscribers = false, bool requireChangeInfo = false, bool requireAddAdmins = false, bool requirePost = false)
    {
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (channel.AdminId == userId)
            return;
        var member = channel.GetMember(userId) ?? throw new UnauthorizedAccessException("Not a channel member.");
        if (!member.IsAdmin)
            throw new UnauthorizedAccessException("Admin rights required.");
        if (requireManageSubscribers && !member.CanManageSubscribers)
            throw new UnauthorizedAccessException("Missing manage-subscribers permission.");
        if (requireChangeInfo && !member.CanChangeInfo)
            throw new UnauthorizedAccessException("Missing change-info permission.");
        if (requireAddAdmins && !member.CanAddAdmins)
            throw new UnauthorizedAccessException("Missing add-admins permission.");
        if (requirePost && !member.CanPost)
            throw new UnauthorizedAccessException("Missing post permission.");
    }
}

public class UpdateChannelProfileCommandHandler : IRequestHandler<UpdateChannelProfileCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateChannelProfileCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateChannelProfileCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireChangeInfo: true);
        channel.UpdateProfile(request.Name, request.Description, request.ProfilePictureUrl);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class SetChannelVisibilityCommandHandler : IRequestHandler<SetChannelVisibilityCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetChannelVisibilityCommandHandler(IGroupRepository groupRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetChannelVisibilityCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireChangeInfo: true);

        if (request.IsPublic)
        {
            if (string.IsNullOrWhiteSpace(request.PublicUsername))
                throw new InvalidOperationException("Public channels require a username.");
            if (await _groupRepository.IsPublicUsernameTakenAsync(request.PublicUsername, channel.Id, cancellationToken))
                throw new InvalidOperationException("Channel username is already taken.");
            var existingUser = await _userRepository.GetByUsernameAsync(request.PublicUsername.Trim().TrimStart('@'));
            if (existingUser != null)
                throw new InvalidOperationException("Channel username conflicts with an existing user.");
        }

        channel.SetVisibility(request.IsPublic, request.PublicUsername);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class SetChannelAdminCommandHandler : IRequestHandler<SetChannelAdminCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetChannelAdminCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetChannelAdminCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireAddAdmins: true);

        var target = channel.GetMember(request.TargetUserId)
            ?? throw new KeyNotFoundException("User is not a subscriber.");
        target.SetChannelAdminRights(
            request.CanPost,
            request.CanEditMessages,
            request.CanDeleteMessages,
            request.CanManageSubscribers,
            request.CanChangeInfo,
            request.CanAddAdmins);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class RemoveChannelAdminCommandHandler : IRequestHandler<RemoveChannelAdminCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveChannelAdminCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveChannelAdminCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireAddAdmins: true);
        if (channel.AdminId == request.TargetUserId)
            throw new InvalidOperationException("Cannot demote the channel owner.");

        var target = channel.GetMember(request.TargetUserId)
            ?? throw new KeyNotFoundException("User is not a subscriber.");
        target.RevokeChannelAdminRights();
        await _unitOfWork.SaveChangesAsync();
    }
}

public class TransferChannelOwnershipCommandHandler : IRequestHandler<TransferChannelOwnershipCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferChannelOwnershipCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TransferChannelOwnershipCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (channel.AdminId != request.CurrentOwnerId)
            throw new UnauthorizedAccessException("Only the owner can transfer ownership.");

        channel.ChangeAdmin(request.NewOwnerId);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class AddChannelSubscriberCommandHandler : IRequestHandler<AddChannelSubscriberCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddChannelSubscriberCommandHandler(IGroupRepository groupRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddChannelSubscriberCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireManageSubscribers: true);
        _ = await _userRepository.GetByIdAsync(request.UserId) ?? throw new KeyNotFoundException("User not found.");
        channel.AddMember(request.UserId, false);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class RemoveChannelSubscriberCommandHandler : IRequestHandler<RemoveChannelSubscriberCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveChannelSubscriberCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveChannelSubscriberCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireManageSubscribers: true);
        if (channel.AdminId == request.UserId)
            throw new InvalidOperationException("Cannot remove the channel owner.");
        channel.RemoveMember(request.UserId);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class ToggleChannelSignaturesCommandHandler : IRequestHandler<ToggleChannelSignaturesCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleChannelSignaturesCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ToggleChannelSignaturesCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireChangeInfo: true);
        channel.SetSignaturesEnabled(request.Enabled);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class RecordChannelMessageViewsCommandHandler : IRequestHandler<RecordChannelMessageViewsCommand, int>
{
    private readonly IMessageViewRepository _messageViewRepository;

    public RecordChannelMessageViewsCommandHandler(IMessageViewRepository messageViewRepository)
    {
        _messageViewRepository = messageViewRepository;
    }

    public Task<int> Handle(RecordChannelMessageViewsCommand request, CancellationToken cancellationToken)
        => _messageViewRepository.RecordViewsAsync(request.UserId, request.MessageIds, cancellationToken);
}

public class LinkDiscussionGroupCommandHandler : IRequestHandler<LinkDiscussionGroupCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LinkDiscussionGroupCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LinkDiscussionGroupCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.AdminId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the owner can link a discussion group.");

        var discussion = await _groupRepository.GetGroupWithMembersAsync(request.DiscussionGroupId)
            ?? throw new KeyNotFoundException("Discussion group not found.");
        if (discussion.Kind != GroupKind.Group)
            throw new InvalidOperationException("Discussion target must be a regular group.");
        if (discussion.AdminId != request.RequestingUserId && !discussion.Members.Any(m => m.UserId == request.RequestingUserId && m.IsAdmin))
            throw new UnauthorizedAccessException("You must admin the discussion group.");

        channel.LinkDiscussionGroup(discussion.Id);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class UnlinkDiscussionGroupCommandHandler : IRequestHandler<UnlinkDiscussionGroupCommand>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnlinkDiscussionGroupCommandHandler(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UnlinkDiscussionGroupCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.AdminId != request.RequestingUserId)
            throw new UnauthorizedAccessException("Only the owner can unlink the discussion group.");
        channel.UnlinkDiscussionGroup();
        await _unitOfWork.SaveChangesAsync();
    }
}

public class SuggestChannelPostCommandHandler : IRequestHandler<SuggestChannelPostCommand, Guid>
{
    private readonly IGroupRepository _groupRepository;
    private readonly ISuggestedPostRepository _suggestedPostRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SuggestChannelPostCommandHandler(IGroupRepository groupRepository, ISuggestedPostRepository suggestedPostRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _suggestedPostRepository = suggestedPostRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SuggestChannelPostCommand request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (!channel.IsMember(request.FromUserId))
            throw new UnauthorizedAccessException("Subscribe to suggest posts.");
        if (channel.CanUserPost(request.FromUserId))
            throw new InvalidOperationException("Admins can post directly; use the composer.");

        var suggestion = new SuggestedPost(request.ChannelId, request.FromUserId, request.Content, request.AttachmentUrl, request.AttachmentType);
        await _suggestedPostRepository.AddAsync(suggestion);
        await _unitOfWork.SaveChangesAsync();
        return suggestion.Id;
    }
}

public class AcceptSuggestedPostCommandHandler : IRequestHandler<AcceptSuggestedPostCommand, Guid>
{
    private readonly ISuggestedPostRepository _suggestedPostRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptSuggestedPostCommandHandler(ISuggestedPostRepository suggestedPostRepository, IMediator mediator, IUnitOfWork unitOfWork)
    {
        _suggestedPostRepository = suggestedPostRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AcceptSuggestedPostCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestedPostRepository.GetByIdWithChannelAsync(request.SuggestedPostId, cancellationToken)
            ?? throw new KeyNotFoundException("Suggested post not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(suggestion.Channel, request.RequestingUserId, requirePost: true);

        var messageId = await _mediator.Send(new SendMessageCommand
        {
            SenderId = request.RequestingUserId,
            GroupId = suggestion.ChannelId,
            Content = suggestion.Content,
            AttachmentUrl = suggestion.AttachmentUrl,
            AttachmentType = suggestion.AttachmentType,
            SendAsChannelId = suggestion.ChannelId
        }, cancellationToken);

        suggestion.Accept(messageId);
        await _unitOfWork.SaveChangesAsync();
        return messageId;
    }
}

public class RejectSuggestedPostCommandHandler : IRequestHandler<RejectSuggestedPostCommand>
{
    private readonly ISuggestedPostRepository _suggestedPostRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectSuggestedPostCommandHandler(ISuggestedPostRepository suggestedPostRepository, IUnitOfWork unitOfWork)
    {
        _suggestedPostRepository = suggestedPostRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RejectSuggestedPostCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestedPostRepository.GetByIdWithChannelAsync(request.SuggestedPostId, cancellationToken)
            ?? throw new KeyNotFoundException("Suggested post not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(suggestion.Channel, request.RequestingUserId, requirePost: true);
        suggestion.Reject(request.AdminNote);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class ScheduleSuggestedPostCommandHandler : IRequestHandler<ScheduleSuggestedPostCommand>
{
    private readonly ISuggestedPostRepository _suggestedPostRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ScheduleSuggestedPostCommandHandler(ISuggestedPostRepository suggestedPostRepository, IUnitOfWork unitOfWork)
    {
        _suggestedPostRepository = suggestedPostRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ScheduleSuggestedPostCommand request, CancellationToken cancellationToken)
    {
        var suggestion = await _suggestedPostRepository.GetByIdWithChannelAsync(request.SuggestedPostId, cancellationToken)
            ?? throw new KeyNotFoundException("Suggested post not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(suggestion.Channel, request.RequestingUserId, requirePost: true);
        suggestion.Schedule(request.ScheduledAt, request.AdminNote);
        await _unitOfWork.SaveChangesAsync();
    }
}
