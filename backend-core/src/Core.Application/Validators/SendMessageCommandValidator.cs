using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SenderId).NotEmpty();

        // Either content or attachment must be provided
        RuleFor(x => x)
            .Must(cmd => !string.IsNullOrWhiteSpace(cmd.Content) || !string.IsNullOrWhiteSpace(cmd.AttachmentUrl))
            .WithMessage("Either Content or AttachmentUrl must be provided.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .When(x => string.IsNullOrWhiteSpace(x.AttachmentUrl))
            .WithMessage("'Content' must not be empty when no attachment is sent.");
        // Basic length check - actual limit enforcement happens in handler using limit resolution service
        // This prevents extremely long messages from being sent (e.g., 100k chars) while allowing configurable limits
        RuleFor(x => x.Content)
            .MaximumLength(100000)
            .When(x => !string.IsNullOrEmpty(x.Content))
            .WithMessage("Message is too long. Maximum allowed length is configured in system limits.");

        RuleFor(x => x)
            .Must(cmd => cmd.ReceiverId.HasValue ^ cmd.GroupId.HasValue)
            .WithMessage("Either ReceiverId or GroupId must be provided, but not both.");

        RuleFor(x => x.ClientMessageId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("ClientMessageId must be a non-empty GUID when provided.");
    }
}

