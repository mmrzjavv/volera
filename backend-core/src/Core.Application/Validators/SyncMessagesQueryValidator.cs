using Core.Application.Queries;
using FluentValidation;

namespace Core.Application.Validators;

public class SyncMessagesQueryValidator : AbstractValidator<SyncMessagesQuery>
{
    public SyncMessagesQueryValidator()
    {
        RuleFor(x => x.CurrentUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.PeerUserId.HasValue ^ x.GroupId.HasValue)
            .WithMessage("Provide exactly one of PeerUserId or GroupId.");
        RuleFor(x => x.Limit).InclusiveBetween(1, 200);
    }
}
