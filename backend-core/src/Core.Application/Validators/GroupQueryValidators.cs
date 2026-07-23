using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class GetUserGroupsQueryValidator : AbstractValidator<GetUserGroupsQuery>
{
    public GetUserGroupsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetGroupByInviteCodeQueryValidator : AbstractValidator<GetGroupByInviteCodeQuery>
{
    public GetGroupByInviteCodeQueryValidator()
    {
        RuleFor(x => x.InviteCode).NotEmpty();
    }
}

public class GetGroupDetailsQueryValidator : AbstractValidator<GetGroupDetailsQuery>
{
    public GetGroupDetailsQueryValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
    }
}

