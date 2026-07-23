using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class InitiateCallCommandValidator : AbstractValidator<InitiateCallCommand>
{
    public InitiateCallCommandValidator()
    {
        RuleFor(x => x.CallerId).NotEmpty();
        RuleFor(x => x.ReceiverId).NotEmpty();
    }
}

