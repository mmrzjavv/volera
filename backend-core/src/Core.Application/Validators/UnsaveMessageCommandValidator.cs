using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class UnsaveMessageCommandValidator : AbstractValidator<UnsaveMessageCommand>
{
    public UnsaveMessageCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty();
    }
}

