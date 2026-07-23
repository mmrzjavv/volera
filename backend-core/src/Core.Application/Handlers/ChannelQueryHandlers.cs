using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class GetMyChannelsQueryHandler : IRequestHandler<GetMyChannelsQuery, List<GroupDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetMyChannelsQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<GroupDto>> Handle(GetMyChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _groupRepository.GetChannelsForUserAsync(request.UserId);
        return channels.Select(g => new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            AdminId = g.AdminId,
            CreatedAt = g.CreatedAt,
            Kind = GroupKind.Channel.ToString(),
            IsChannel = true,
            ProfilePictureUrl = _fileStorage.ResolveClientUrl(g.ProfilePictureUrl),
            IsPublic = g.IsPublic,
            PublicUsername = g.PublicUsername,
            CanPost = g.CanUserPost(request.UserId)
        }).ToList();
    }
}

public class GetChannelDetailsQueryHandler : IRequestHandler<GetChannelDetailsQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetChannelDetailsQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<GroupDetailsDto> Handle(GetChannelDetailsQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Not a channel.");
        if (!channel.IsMember(request.CurrentUserId))
            throw new UnauthorizedAccessException("You are not subscribed to this channel.");

        return MapChannelDetails(channel, request.CurrentUserId, includeMembers: true, _fileStorage);
    }

    internal static GroupDetailsDto MapChannelDetails(
        Group channel,
        Guid? currentUserId,
        bool includeMembers,
        IFileStorageService fileStorage)
    {
        var member = currentUserId.HasValue ? channel.GetMember(currentUserId.Value) : null;
        var isOwner = currentUserId.HasValue && channel.AdminId == currentUserId.Value;
        var isAdmin = isOwner || (member?.IsAdmin ?? false);

        var dto = new GroupDetailsDto
        {
            Id = channel.Id,
            Name = channel.Name,
            AdminId = channel.AdminId,
            CreatedAt = channel.CreatedAt,
            Description = channel.Description,
            ProfilePictureUrl = fileStorage.ResolveClientUrl(channel.ProfilePictureUrl),
            InviteCode = isAdmin ? channel.InviteCode : null,
            Kind = GroupKind.Channel.ToString(),
            IsChannel = true,
            IsPublic = channel.IsPublic,
            PublicUsername = channel.PublicUsername,
            SignaturesEnabled = channel.SignaturesEnabled,
            LinkedDiscussionGroupId = channel.LinkedDiscussionGroupId,
            SubscriberCount = channel.Members.Count,
            CanPost = isOwner || (member?.CanPost ?? false),
            IsAdmin = isAdmin,
            CanManageSubscribers = isOwner || (member?.CanManageSubscribers ?? false),
            CanChangeInfo = isOwner || (member?.CanChangeInfo ?? false),
            CanAddAdmins = isOwner || (member?.CanAddAdmins ?? false),
            CanEditMessages = isOwner || (member?.CanEditMessages ?? false),
            CanDeleteMessages = isOwner || (member?.CanDeleteMessages ?? false)
        };

        if (includeMembers && isAdmin)
        {
            dto.Admins = channel.Members
                .Where(m => m.IsAdmin || m.UserId == channel.AdminId)
                .Select(m => MapMember(m, fileStorage))
                .ToList();
            dto.Members = channel.Members
                .OrderBy(m => m.JoinedAt)
                .Select(m => new UserDto
                {
                    Id = m.UserId,
                    Username = m.User?.Username ?? string.Empty,
                    FirstName = m.User?.FirstName ?? string.Empty,
                    LastName = m.User?.LastName ?? string.Empty,
                    PhoneNumber = m.User?.PhoneNumber ?? string.Empty,
                    ProfilePicture = fileStorage.ResolveClientUrl(m.User?.ProfilePicture),
                    IsOnline = false
                }).ToList();
        }

        return dto;
    }

    private static ChannelMemberDto MapMember(GroupMember m, IFileStorageService fileStorage) => new()
    {
        UserId = m.UserId,
        Username = m.User?.Username ?? string.Empty,
        FirstName = m.User?.FirstName ?? string.Empty,
        LastName = m.User?.LastName ?? string.Empty,
        ProfilePicture = fileStorage.ResolveClientUrl(m.User?.ProfilePicture),
        IsAdmin = m.IsAdmin,
        CanPost = m.CanPost,
        CanEditMessages = m.CanEditMessages,
        CanDeleteMessages = m.CanDeleteMessages,
        CanManageSubscribers = m.CanManageSubscribers,
        CanChangeInfo = m.CanChangeInfo,
        CanAddAdmins = m.CanAddAdmins,
        JoinedAt = m.JoinedAt
    };
}

public class GetChannelInvitePreviewQueryHandler : IRequestHandler<GetChannelInvitePreviewQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetChannelInvitePreviewQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<GroupDetailsDto> Handle(GetChannelInvitePreviewQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetByInviteCodeAsync(request.InviteCode)
            ?? throw new KeyNotFoundException("Invite not found.");
        if (channel.Kind != GroupKind.Channel)
            throw new InvalidOperationException("Invite is not for a channel.");

        return new GroupDetailsDto
        {
            Id = channel.Id,
            Name = channel.Name,
            AdminId = channel.AdminId,
            CreatedAt = channel.CreatedAt,
            Description = channel.Description,
            ProfilePictureUrl = _fileStorage.ResolveClientUrl(channel.ProfilePictureUrl),
            Kind = GroupKind.Channel.ToString(),
            IsChannel = true,
            IsPublic = channel.IsPublic,
            PublicUsername = channel.PublicUsername,
            SubscriberCount = channel.Members.Count
        };
    }
}

public class SearchPublicChannelsQueryHandler : IRequestHandler<SearchPublicChannelsQuery, List<PublicChannelSearchResultDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public SearchPublicChannelsQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<PublicChannelSearchResultDto>> Handle(SearchPublicChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _groupRepository.SearchPublicChannelsAsync(request.Query, request.Limit);
        var results = new List<PublicChannelSearchResultDto>();
        foreach (var c in channels)
        {
            results.Add(new PublicChannelSearchResultDto
            {
                Id = c.Id,
                Name = c.Name,
                PublicUsername = c.PublicUsername,
                Description = c.Description,
                ProfilePictureUrl = _fileStorage.ResolveClientUrl(c.ProfilePictureUrl),
                SubscriberCount = await _groupRepository.GetSubscriberCountAsync(c.Id, cancellationToken)
            });
        }
        return results;
    }
}

public class GetChannelSubscribersQueryHandler : IRequestHandler<GetChannelSubscribersQuery, List<ChannelMemberDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetChannelSubscribersQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<ChannelMemberDto>> Handle(GetChannelSubscribersQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId, requireManageSubscribers: true);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        return channel.Members
            .OrderBy(m => m.JoinedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChannelMemberDto
            {
                UserId = m.UserId,
                Username = m.User?.Username ?? string.Empty,
                FirstName = m.User?.FirstName ?? string.Empty,
                LastName = m.User?.LastName ?? string.Empty,
                ProfilePicture = _fileStorage.ResolveClientUrl(m.User?.ProfilePicture),
                IsAdmin = m.IsAdmin,
                CanPost = m.CanPost,
                CanEditMessages = m.CanEditMessages,
                CanDeleteMessages = m.CanDeleteMessages,
                CanManageSubscribers = m.CanManageSubscribers,
                CanChangeInfo = m.CanChangeInfo,
                CanAddAdmins = m.CanAddAdmins,
                JoinedAt = m.JoinedAt
            }).ToList();
    }
}

public class GetChannelAnalyticsQueryHandler : IRequestHandler<GetChannelAnalyticsQuery, ChannelAnalyticsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMessageRepository _messageRepository;

    public GetChannelAnalyticsQueryHandler(IGroupRepository groupRepository, IMessageRepository messageRepository)
    {
        _groupRepository = groupRepository;
        _messageRepository = messageRepository;
    }

    public async Task<ChannelAnalyticsDto> Handle(GetChannelAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId);

        var messages = (await _messageRepository.GetGroupMessagesAsync(request.ChannelId, 5000, null)).ToList();
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        return new ChannelAnalyticsDto
        {
            ChannelId = channel.Id,
            SubscriberCount = channel.Members.Count,
            PostCount = messages.Count,
            TotalViews = messages.Sum(m => (long)m.ViewCount),
            PostsLast7Days = messages.Count(m => m.SentAt >= weekAgo)
        };
    }
}

public class ListSuggestedPostsQueryHandler : IRequestHandler<ListSuggestedPostsQuery, List<SuggestedPostDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly ISuggestedPostRepository _suggestedPostRepository;

    public ListSuggestedPostsQueryHandler(IGroupRepository groupRepository, ISuggestedPostRepository suggestedPostRepository)
    {
        _groupRepository = groupRepository;
        _suggestedPostRepository = suggestedPostRepository;
    }

    public async Task<List<SuggestedPostDto>> Handle(ListSuggestedPostsQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetGroupWithMembersAsync(request.ChannelId)
            ?? throw new KeyNotFoundException("Channel not found.");
        GenerateChannelInviteLinkCommandHandler.EnsureChannelAdmin(channel, request.RequestingUserId);

        SuggestedPostStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<SuggestedPostStatus>(request.Status, true, out var parsed))
            status = parsed;

        var items = await _suggestedPostRepository.GetByChannelAsync(request.ChannelId, status, cancellationToken);
        return items.Select(s => new SuggestedPostDto
        {
            Id = s.Id,
            ChannelId = s.ChannelId,
            FromUserId = s.FromUserId,
            FromUserName = s.FromUser != null ? $"{s.FromUser.FirstName} {s.FromUser.LastName}".Trim() : string.Empty,
            Content = s.Content,
            AttachmentUrl = s.AttachmentUrl,
            AttachmentType = s.AttachmentType,
            Status = s.Status.ToString(),
            ScheduledAt = s.ScheduledAt,
            AdminNote = s.AdminNote,
            PublishedMessageId = s.PublishedMessageId,
            CreatedAt = s.CreatedAt
        }).ToList();
    }
}

public class GetChannelByUsernameQueryHandler : IRequestHandler<GetChannelByUsernameQuery, GroupDetailsDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public GetChannelByUsernameQueryHandler(IGroupRepository groupRepository, IFileStorageService fileStorage)
    {
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<GroupDetailsDto> Handle(GetChannelByUsernameQuery request, CancellationToken cancellationToken)
    {
        var channel = await _groupRepository.GetByPublicUsernameAsync(request.PublicUsername)
            ?? throw new KeyNotFoundException("Channel not found.");
        if (!channel.IsPublic)
            throw new UnauthorizedAccessException("Channel is private.");

        if (request.CurrentUserId.HasValue && channel.IsMember(request.CurrentUserId.Value))
            return GetChannelDetailsQueryHandler.MapChannelDetails(channel, request.CurrentUserId, includeMembers: true, _fileStorage);

        return new GroupDetailsDto
        {
            Id = channel.Id,
            Name = channel.Name,
            AdminId = channel.AdminId,
            CreatedAt = channel.CreatedAt,
            Description = channel.Description,
            ProfilePictureUrl = _fileStorage.ResolveClientUrl(channel.ProfilePictureUrl),
            Kind = GroupKind.Channel.ToString(),
            IsChannel = true,
            IsPublic = true,
            PublicUsername = channel.PublicUsername,
            SubscriberCount = channel.Members.Count
        };
    }
}
