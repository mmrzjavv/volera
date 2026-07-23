using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, Guid>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoryNotificationService _storyNotificationService;

    public CreateStoryCommandHandler(
        IStoryRepository storyRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IStoryNotificationService storyNotificationService)
    {
        _storyRepository = storyRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _storyNotificationService = storyNotificationService;
    }

    public async Task<Guid> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var story = new Story(request.UserId, expiresAt);

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var duration = item.MediaType == "Video"
                ? item.DurationMs!.Value
                : item.DurationMs ?? 5000;
            story.AddItem(item.MediaType, item.ObjectKey, duration, item.TextOverlayJson, i);
        }

        await _storyRepository.AddAsync(story);
        await _unitOfWork.SaveChangesAsync();

        var contacts = await _contactRepository.GetContactsByUserIdAsync(request.UserId);
        var contactIds = contacts
            .Where(c => c.Status == ContactStatus.Accepted && c.ContactUserId.HasValue)
            .Select(c => c.ContactUserId!.Value)
            .ToList();

        await _storyNotificationService.NotifyStoryCreated(request.UserId, story.Id, contactIds);

        return story.Id;
    }
}

public class MarkStoryViewedCommandHandler : IRequestHandler<MarkStoryViewedCommand>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkStoryViewedCommandHandler(IStoryRepository storyRepository, IUnitOfWork unitOfWork)
    {
        _storyRepository = storyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkStoryViewedCommand request, CancellationToken cancellationToken)
    {
        var story = await _storyRepository.GetByIdWithItemsAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Story not found.");

        if (!story.IsActive(DateTime.UtcNow))
            throw new InvalidOperationException("Story is no longer available.");

        if (story.UserId == request.ViewerUserId)
            return;

        var existing = await _storyRepository.GetViewAsync(request.StoryId, request.ViewerUserId, cancellationToken);
        if (existing != null)
        {
            existing.Touch();
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        await _storyRepository.AddViewAsync(new StoryView(request.StoryId, request.ViewerUserId), cancellationToken);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoryNotificationService _storyNotificationService;

    public DeleteStoryCommandHandler(
        IStoryRepository storyRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IStoryNotificationService storyNotificationService)
    {
        _storyRepository = storyRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _storyNotificationService = storyNotificationService;
    }

    public async Task Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _storyRepository.GetByIdWithItemsAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Story not found.");

        if (story.UserId != request.UserId)
            throw new UnauthorizedAccessException("Only the author can delete this story.");

        story.SoftDelete();
        await _unitOfWork.SaveChangesAsync();

        var contacts = await _contactRepository.GetContactsByUserIdAsync(request.UserId);
        var contactIds = contacts
            .Where(c => c.Status == ContactStatus.Accepted && c.ContactUserId.HasValue)
            .Select(c => c.ContactUserId!.Value)
            .ToList();

        await _storyNotificationService.NotifyStoryDeleted(request.UserId, story.Id, contactIds);
    }
}

public class DeleteStoryItemCommandHandler : IRequestHandler<DeleteStoryItemCommand>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoryNotificationService _storyNotificationService;

    public DeleteStoryItemCommandHandler(
        IStoryRepository storyRepository,
        IContactRepository contactRepository,
        IUnitOfWork unitOfWork,
        IStoryNotificationService storyNotificationService)
    {
        _storyRepository = storyRepository;
        _contactRepository = contactRepository;
        _unitOfWork = unitOfWork;
        _storyNotificationService = storyNotificationService;
    }

    public async Task Handle(DeleteStoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _storyRepository.GetItemByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Story item not found.");

        var story = await _storyRepository.GetByIdWithItemsAsync(item.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Story not found.");

        if (story.UserId != request.UserId)
            throw new UnauthorizedAccessException("Only the author can delete this item.");

        _storyRepository.RemoveItem(item);

        var remaining = story.Items.Count(i => i.Id != item.Id);
        if (remaining == 0)
            story.SoftDelete();

        await _unitOfWork.SaveChangesAsync();

        if (story.DeletedAt.HasValue)
        {
            var contacts = await _contactRepository.GetContactsByUserIdAsync(request.UserId);
            var contactIds = contacts
                .Where(c => c.Status == ContactStatus.Accepted && c.ContactUserId.HasValue)
                .Select(c => c.ContactUserId!.Value)
                .ToList();
            await _storyNotificationService.NotifyStoryDeleted(request.UserId, story.Id, contactIds);
        }
    }
}

public class ReplyToStoryItemCommandHandler : IRequestHandler<ReplyToStoryItemCommand, Guid>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IMediator _mediator;

    public ReplyToStoryItemCommandHandler(IStoryRepository storyRepository, IMediator mediator)
    {
        _storyRepository = storyRepository;
        _mediator = mediator;
    }

    public async Task<Guid> Handle(ReplyToStoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _storyRepository.GetItemByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Story item not found.");

        var story = item.Story;
        if (story == null || !story.IsActive(DateTime.UtcNow))
            throw new InvalidOperationException("Story is no longer available.");

        if (story.UserId == request.SenderId)
            throw new InvalidOperationException("Cannot reply to your own story.");

        // Reuse message pipeline via SendMessageCommand, then attach story item reply on a follow-up is hard.
        // Send via MediatR SendMessageCommand and then we need SetReplyToStoryItem — extend SendMessage instead.
        var messageId = await _mediator.Send(new SendMessageCommand
        {
            SenderId = request.SenderId,
            ReceiverId = story.UserId,
            Content = request.Content,
            ReplyToStoryItemId = item.Id,
            AttachmentUrl = item.ObjectKey,
            AttachmentType = item.MediaType == "Video" ? "video/mp4" : "image/jpeg"
        }, cancellationToken);

        return messageId;
    }
}

public class ExpireStoriesCommandHandler : IRequestHandler<ExpireStoriesCommand, int>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpireStoriesCommandHandler(IStoryRepository storyRepository, IUnitOfWork unitOfWork)
    {
        _storyRepository = storyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ExpireStoriesCommand request, CancellationToken cancellationToken)
    {
        await _storyRepository.SoftDeleteExpiredAsync(DateTime.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return 0;
    }
}
