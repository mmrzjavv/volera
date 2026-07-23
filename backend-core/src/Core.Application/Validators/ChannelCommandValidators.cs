using Core.Application.Commands;
using FluentValidation;

namespace Core.Application.Validators;

public class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CreatorId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        RuleFor(x => x.PublicUsername)
            .NotEmpty()
            .MaximumLength(64)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]{3,31}$")
            .When(x => x.IsPublic);
    }
}

public class SubscribeToChannelCommandValidator : AbstractValidator<SubscribeToChannelCommand>
{
    public SubscribeToChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class LeaveChannelCommandValidator : AbstractValidator<LeaveChannelCommand>
{
    public LeaveChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class JoinChannelByInviteCommandValidator : AbstractValidator<JoinChannelByInviteCommand>
{
    public JoinChannelByInviteCommandValidator()
    {
        RuleFor(x => x.InviteCode).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GenerateChannelInviteLinkCommandValidator : AbstractValidator<GenerateChannelInviteLinkCommand>
{
    public GenerateChannelInviteLinkCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class UpdateChannelProfileCommandValidator : AbstractValidator<UpdateChannelProfileCommand>
{
    public UpdateChannelProfileCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class SetChannelVisibilityCommandValidator : AbstractValidator<SetChannelVisibilityCommand>
{
    public SetChannelVisibilityCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.PublicUsername)
            .NotEmpty()
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]{3,31}$")
            .When(x => x.IsPublic);
    }
}

public class SetChannelAdminCommandValidator : AbstractValidator<SetChannelAdminCommand>
{
    public SetChannelAdminCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}

public class RemoveChannelAdminCommandValidator : AbstractValidator<RemoveChannelAdminCommand>
{
    public RemoveChannelAdminCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}

public class TransferChannelOwnershipCommandValidator : AbstractValidator<TransferChannelOwnershipCommand>
{
    public TransferChannelOwnershipCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.CurrentOwnerId).NotEmpty();
        RuleFor(x => x.NewOwnerId).NotEmpty();
    }
}

public class AddChannelSubscriberCommandValidator : AbstractValidator<AddChannelSubscriberCommand>
{
    public AddChannelSubscriberCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class RemoveChannelSubscriberCommandValidator : AbstractValidator<RemoveChannelSubscriberCommand>
{
    public RemoveChannelSubscriberCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ToggleChannelSignaturesCommandValidator : AbstractValidator<ToggleChannelSignaturesCommand>
{
    public ToggleChannelSignaturesCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class RecordChannelMessageViewsCommandValidator : AbstractValidator<RecordChannelMessageViewsCommand>
{
    public RecordChannelMessageViewsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MessageIds).NotNull();
    }
}

public class LinkDiscussionGroupCommandValidator : AbstractValidator<LinkDiscussionGroupCommand>
{
    public LinkDiscussionGroupCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.DiscussionGroupId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class UnlinkDiscussionGroupCommandValidator : AbstractValidator<UnlinkDiscussionGroupCommand>
{
    public UnlinkDiscussionGroupCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class SuggestChannelPostCommandValidator : AbstractValidator<SuggestChannelPostCommand>
{
    public SuggestChannelPostCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.FromUserId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}

public class AcceptSuggestedPostCommandValidator : AbstractValidator<AcceptSuggestedPostCommand>
{
    public AcceptSuggestedPostCommandValidator()
    {
        RuleFor(x => x.SuggestedPostId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class RejectSuggestedPostCommandValidator : AbstractValidator<RejectSuggestedPostCommand>
{
    public RejectSuggestedPostCommandValidator()
    {
        RuleFor(x => x.SuggestedPostId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public class ScheduleSuggestedPostCommandValidator : AbstractValidator<ScheduleSuggestedPostCommand>
{
    public ScheduleSuggestedPostCommandValidator()
    {
        RuleFor(x => x.SuggestedPostId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.ScheduledAt).Must(d => d > DateTime.UtcNow).WithMessage("Scheduled time must be in the future.");
    }
}
