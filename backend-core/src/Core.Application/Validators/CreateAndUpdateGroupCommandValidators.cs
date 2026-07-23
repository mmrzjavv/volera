using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CreatorId).NotEmpty();
        RuleFor(x => x.MemberIds).NotNull();
    }
}

public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}

public class GenerateGroupInviteLinkCommandValidator : AbstractValidator<GenerateGroupInviteLinkCommand>
{
    public GenerateGroupInviteLinkCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class ChangeGroupAdminCommandValidator : AbstractValidator<ChangeGroupAdminCommand>
{
    public ChangeGroupAdminCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.CurrentAdminId).NotEmpty();
        RuleFor(x => x.NewAdminId).NotEmpty();
    }
}public class DeleteGroupCommandValidator : AbstractValidator<DeleteGroupCommand>
{
    public DeleteGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class UpdateGroupProfileCommandValidator : AbstractValidator<UpdateGroupProfileCommand>
{
    public UpdateGroupProfileCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Name));
    }
}

public class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}
