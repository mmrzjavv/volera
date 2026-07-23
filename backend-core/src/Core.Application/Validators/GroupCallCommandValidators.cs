using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class InitiateGroupCallCommandValidator : AbstractValidator<InitiateGroupCallCommand>
{
    public InitiateGroupCallCommandValidator()
    {
        RuleFor(x => x.InitiatorId).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
    }
}

