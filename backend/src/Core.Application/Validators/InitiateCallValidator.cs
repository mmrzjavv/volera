using FluentValidation;
using Core.Application.DTOs;

namespace Core.Application.Validators;

public class InitiateCallValidator : AbstractValidator<InitiateCallDto>
{
    public InitiateCallValidator()
    {
        RuleFor(x => x.ReceiverId).NotEmpty();
    }
}