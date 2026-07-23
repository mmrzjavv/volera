using FluentValidation;
using Core.Application.DTOs;

namespace Core.Application.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100);
        RuleFor(x => x.Bio).MaximumLength(500);
    }
}