using FluentValidation;
using Core.Application.Commands;

namespace Core.Application.Validators;

public class CreateStoryCommandValidator : AbstractValidator<CreateStoryCommand>
{
    public CreateStoryCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one story item is required.")
            .Must(i => i.Count <= 10).WithMessage("Maximum 10 items per story.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ObjectKey).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.MediaType)
                .NotEmpty()
                .Must(m => m is "Image" or "Video")
                .WithMessage("MediaType must be Image or Video.");
            item.RuleFor(i => i.TextOverlayJson).MaximumLength(2000).When(i => i.TextOverlayJson != null);
            item.RuleFor(i => i.DurationMs)
                .Must((dto, ms) =>
                {
                    if (dto.MediaType == "Video")
                        return ms is > 0 and <= 15000;
                    return ms is null or (>= 1000 and <= 15000);
                })
                .WithMessage("Invalid duration for media type.");
        });
    }
}

public class MarkStoryViewedCommandValidator : AbstractValidator<MarkStoryViewedCommand>
{
    public MarkStoryViewedCommandValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
        RuleFor(x => x.ViewerUserId).NotEmpty();
    }
}

public class DeleteStoryCommandValidator : AbstractValidator<DeleteStoryCommand>
{
    public DeleteStoryCommandValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class DeleteStoryItemCommandValidator : AbstractValidator<DeleteStoryItemCommand>
{
    public DeleteStoryItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class ReplyToStoryItemCommandValidator : AbstractValidator<ReplyToStoryItemCommand>
{
    public ReplyToStoryItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.SenderId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

public class GetStoryFeedQueryValidator : AbstractValidator<Queries.GetStoryFeedQuery>
{
    public GetStoryFeedQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class GetUserStoriesQueryValidator : AbstractValidator<Queries.GetUserStoriesQuery>
{
    public GetUserStoriesQueryValidator()
    {
        RuleFor(x => x.ViewerUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}

public class GetStoryViewersQueryValidator : AbstractValidator<Queries.GetStoryViewersQuery>
{
    public GetStoryViewersQueryValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
        RuleFor(x => x.RequesterId).NotEmpty();
    }
}

public class ExpireStoriesCommandValidator : AbstractValidator<ExpireStoriesCommand>
{
    public ExpireStoriesCommandValidator() { }
}
