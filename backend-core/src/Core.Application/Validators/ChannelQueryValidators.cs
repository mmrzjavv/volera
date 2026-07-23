using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetMyChannelsQueryValidator : AbstractValidator<GetMyChannelsQuery>
{
    public GetMyChannelsQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public class GetChannelDetailsQueryValidator : AbstractValidator<GetChannelDetailsQuery>
{
    public GetChannelDetailsQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.CurrentUserId).NotEmpty();
    }
}

public class GetChannelInvitePreviewQueryValidator : AbstractValidator<GetChannelInvitePreviewQuery>
{
    public GetChannelInvitePreviewQueryValidator() => RuleFor(x => x.InviteCode).NotEmpty();
}

public class SearchPublicChannelsQueryValidator : AbstractValidator<SearchPublicChannelsQuery>
{
    public SearchPublicChannelsQueryValidator() => RuleFor(x => x.Query).NotNull();
}

public class GetChannelSubscribersQueryValidator : AbstractValidator<GetChannelSubscribersQuery>
{
    public GetChannelSubscribersQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class GetChannelAnalyticsQueryValidator : AbstractValidator<GetChannelAnalyticsQuery>
{
    public GetChannelAnalyticsQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class ListSuggestedPostsQueryValidator : AbstractValidator<ListSuggestedPostsQuery>
{
    public ListSuggestedPostsQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class GetChannelByUsernameQueryValidator : AbstractValidator<GetChannelByUsernameQuery>
{
    public GetChannelByUsernameQueryValidator() => RuleFor(x => x.PublicUsername).NotEmpty();
}
