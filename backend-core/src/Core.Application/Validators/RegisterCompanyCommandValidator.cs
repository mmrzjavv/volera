using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    public RegisterCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MobileNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Email).MaximumLength(256).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Address));
    }
}
