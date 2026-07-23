using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class SearchUserByPhoneNumberQueryValidator : AbstractValidator<SearchUserByPhoneNumberQuery>
{
    public SearchUserByPhoneNumberQueryValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
    }
}

public class SearchUserByUsernameQueryValidator : AbstractValidator<SearchUserByUsernameQuery>
{
    public SearchUserByUsernameQueryValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(20);
    }
}

