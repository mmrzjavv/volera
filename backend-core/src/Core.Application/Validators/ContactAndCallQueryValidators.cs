using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetContactsQueryValidator : AbstractValidator<GetContactsQuery>
{
    public GetContactsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetCallsByUserIdQueryValidator : AbstractValidator<GetCallsByUserIdQuery>
{
    public GetCallsByUserIdQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}

