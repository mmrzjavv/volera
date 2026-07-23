using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class GetGroupMessagesQueryValidator : AbstractValidator<GetGroupMessagesQuery>
{
    public GetGroupMessagesQueryValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

