using Core.Application.Commands;
using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class JoinGroupByInviteCommandValidator : AbstractValidator<JoinGroupByInviteCommand>
{
    public JoinGroupByInviteCommandValidator()
    {
        RuleFor(x => x.InviteCode).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ForwardMessageCommandValidator : AbstractValidator<ForwardMessageCommand>
{
    public ForwardMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x)
            .Must(cmd => cmd.ReceiverId.HasValue ^ cmd.GroupId.HasValue)
            .WithMessage("Either ReceiverId or GroupId must be provided, but not both.");
    }
}

