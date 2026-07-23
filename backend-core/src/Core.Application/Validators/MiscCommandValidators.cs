using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class MarkMessagesAsReadCommandValidator : AbstractValidator<MarkMessagesAsReadCommand>
{
    public MarkMessagesAsReadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();
    }
}

public class DeleteOfflineUsersCommandValidator : AbstractValidator<DeleteOfflineUsersCommand>
{
    public DeleteOfflineUsersCommandValidator()
    {
        // No inputs; included for completeness to satisfy validation requirement.
    }
}

public class RemoveChatFromRecentCommandValidator : AbstractValidator<RemoveChatFromRecentCommand>
{
    public RemoveChatFromRecentCommandValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x).Must(x => x.OtherUserId.HasValue || x.GroupId.HasValue)
            .WithMessage("Either OtherUserId or GroupId must be set.");
    }
}

public class PinMessageCommandValidator : AbstractValidator<PinMessageCommand>
{
    public PinMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class UnpinMessageCommandValidator : AbstractValidator<UnpinMessageCommand>
{
    public UnpinMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class AddOrUpdateReactionCommandValidator : AbstractValidator<AddOrUpdateReactionCommand>
{
    public AddOrUpdateReactionCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Emoji).NotEmpty().MaximumLength(50);
    }
}

public class RemoveReactionCommandValidator : AbstractValidator<RemoveReactionCommand>
{
    public RemoveReactionCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

