using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

