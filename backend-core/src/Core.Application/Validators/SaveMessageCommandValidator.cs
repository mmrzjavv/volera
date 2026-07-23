using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class SaveMessageCommandValidator : AbstractValidator<SaveMessageCommand>
{
    public SaveMessageCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}

