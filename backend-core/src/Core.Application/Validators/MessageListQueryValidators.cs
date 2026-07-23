using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetRecentChatsQueryValidator : AbstractValidator<GetRecentChatsQuery>
{
    public GetRecentChatsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetSavedMessagesQueryValidator : AbstractValidator<GetSavedMessagesQuery>
{
    public GetSavedMessagesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}

public class GetUnreadCountsQueryValidator : AbstractValidator<GetUnreadCountsQuery>
{
    public GetUnreadCountsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetTotalMessagesCountQueryValidator : AbstractValidator<GetTotalMessagesCountQuery>
{
    public GetTotalMessagesCountQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

