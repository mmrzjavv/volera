using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.ContactIdentifier).NotEmpty();
        RuleFor(x => x.ContactName).NotEmpty();
    }
}

public class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ContactId).NotEmpty();
    }
}

public class SyncContactsCommandValidator : AbstractValidator<SyncContactsCommand>
{
    public SyncContactsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PhoneNumbers).NotNull();
    }
}

