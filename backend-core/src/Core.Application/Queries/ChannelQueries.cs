using Core.Application.DTOs;
using MediatR;

namespace Core.Application.Queries;

public class GetMyChannelsQuery : IRequest<List<GroupDto>>
{
    public Guid UserId { get; set; }
}

public class GetChannelDetailsQuery : IRequest<GroupDetailsDto>
{
    public Guid ChannelId { get; set; }
    public Guid CurrentUserId { get; set; }
}

public class GetChannelInvitePreviewQuery : IRequest<GroupDetailsDto>
{
    public required string InviteCode { get; set; }
}

public class SearchPublicChannelsQuery : IRequest<List<PublicChannelSearchResultDto>>
{
    public required string Query { get; set; }
    public int Limit { get; set; } = 20;
}

public class GetChannelSubscribersQuery : IRequest<List<ChannelMemberDto>>
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetChannelAnalyticsQuery : IRequest<ChannelAnalyticsDto>
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
}

public class ListSuggestedPostsQuery : IRequest<List<SuggestedPostDto>>
{
    public Guid ChannelId { get; set; }
    public Guid RequestingUserId { get; set; }
    public string? Status { get; set; }
}

public class GetChannelByUsernameQuery : IRequest<GroupDetailsDto>
{
    public required string PublicUsername { get; set; }
    public Guid? CurrentUserId { get; set; }
}
