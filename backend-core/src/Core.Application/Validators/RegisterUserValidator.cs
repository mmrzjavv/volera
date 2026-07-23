using FluentValidation;
using Core.Application.DTOs;

namespace Core.Application.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(20);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[1-9]\d{1,14}$");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}