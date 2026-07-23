using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class CreateGuestSessionCommandValidator : AbstractValidator<CreateGuestSessionCommand>
{
    public CreateGuestSessionCommandValidator()
    {
        RuleFor(x => x)
            .Must(c => !string.IsNullOrWhiteSpace(c.Email) || !string.IsNullOrWhiteSpace(c.Mobile))
            .WithMessage("At least one of Email or Mobile must be provided.");

        RuleFor(x => x.FirstName).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.LastName));
        RuleFor(x => x.Email).MaximumLength(255).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Mobile).MaximumLength(15).When(x => !string.IsNullOrEmpty(x.Mobile));
    }
}
