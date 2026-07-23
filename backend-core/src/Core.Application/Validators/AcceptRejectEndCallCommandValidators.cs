using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class AcceptCallCommandValidator : AbstractValidator<AcceptCallCommand>
{
    public AcceptCallCommandValidator()
    {
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class RejectCallCommandValidator : AbstractValidator<RejectCallCommand>
{
    public RejectCallCommandValidator()
    {
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class EndCallCommandValidator : AbstractValidator<EndCallCommand>
{
    public EndCallCommandValidator()
    {
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

