using Core.Application.Commands.SystemMessages;
using Core.Application.Queries.SystemMessages;
using FluentValidation;

namespace Core.Application.Validators;

public class GetActiveSystemMessagesQueryValidator : AbstractValidator<GetActiveSystemMessagesQuery>
{
    public GetActiveSystemMessagesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class CreateSystemMessageCommandValidator : AbstractValidator<CreateSystemMessageCommand>
{
    public CreateSystemMessageCommandValidator()
    {
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public class UpdateSystemMessageCommandValidator : AbstractValidator<UpdateSystemMessageCommand>
{
    public UpdateSystemMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.AuthorId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public class MarkSystemMessageReadCommandValidator : AbstractValidator<MarkSystemMessageReadCommand>
{
    public MarkSystemMessageReadCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeleteSystemMessageCommandValidator : AbstractValidator<DeleteSystemMessageCommand>
{
    public DeleteSystemMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.AuthorId).NotEmpty();
    }
}
