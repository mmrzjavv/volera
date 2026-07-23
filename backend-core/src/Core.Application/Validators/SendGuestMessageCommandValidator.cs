using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class SendGuestMessageCommandValidator : AbstractValidator<SendGuestMessageCommand>
{
    public SendGuestMessageCommandValidator()
    {
        RuleFor(x => x.GuestToken).NotEmpty().WithMessage("Guest token is required.");

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
    }
}
