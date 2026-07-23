using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewContent).NotEmpty().MaximumLength(2000);
    }
}

