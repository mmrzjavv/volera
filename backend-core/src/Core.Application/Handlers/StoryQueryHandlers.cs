using MediatR;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Application.Queries;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetStoryFeedQueryHandler : IRequestHandler<GetStoryFeedQuery, List<StoryRingDto>>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileStorageService _fileStorage;

    public GetStoryFeedQueryHandler(
        IStoryRepository storyRepository,
        IContactRepository contactRepository,
        IUserRepository userRepository,
        IFileStorageService fileStorage)
    {
        _storyRepository = storyRepository;
        _contactRepository = contactRepository;
        _userRepository = userRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<StoryRingDto>> Handle(GetStoryFeedQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var contacts = await _contactRepository.GetContactsByUserIdAsync(request.UserId);
        var contactUserIds = contacts
            .Where(c => c.Status == ContactStatus.Accepted && c.ContactUserId.HasValue)
            .Select(c => c.ContactUserId!.Value)
            .Distinct()
            .ToList();

        var userIds = contactUserIds.Append(request.UserId).Distinct().ToList();
        var stories = await _storyRepository.GetActiveStoriesForUsersAsync(userIds, now, cancellationToken);

        var rings = new List<StoryRingDto>();
        foreach (var group in stories.GroupBy(s => s.UserId))
        {
            var user = group.First().User ?? await _userRepository.GetByIdAsync(group.Key);
            if (user == null) continue;

            var storyDtos = group.Select(s => MapStory(s, request.UserId)).ToList();
            bool hasUnseen;
            if (group.Key == request.UserId)
                hasUnseen = storyDtos.Count > 0;
            else
                hasUnseen = storyDtos.Any(s => !s.ViewedByMe);

            rings.Add(new StoryRingDto
            {
                UserId = group.Key,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                ProfilePicture = _fileStorage.ResolveClientUrl(user.ProfilePicture),
                HasUnseen = hasUnseen,
                IsOwn = group.Key == request.UserId,
                LatestCreatedAt = group.Max(s => s.CreatedAt),
                Stories = storyDtos
            });
        }

        // Ensure own ring appears even with no stories (for "Add story")
        if (!rings.Any(r => r.IsOwn))
        {
            var me = await _userRepository.GetByIdAsync(request.UserId);
            if (me != null)
            {
                rings.Insert(0, new StoryRingDto
                {
                    UserId = me.Id,
                    DisplayName = $"{me.FirstName} {me.LastName}".Trim(),
                    ProfilePicture = _fileStorage.ResolveClientUrl(me.ProfilePicture),
                    HasUnseen = false,
                    IsOwn = true,
                    LatestCreatedAt = DateTime.MinValue,
                    Stories = new List<StoryDto>()
                });
            }
        }

        return rings
            .OrderByDescending(r => r.IsOwn)
            .ThenByDescending(r => r.HasUnseen)
            .ThenByDescending(r => r.LatestCreatedAt)
            .ToList();
    }

    private StoryDto MapStory(Story s, Guid viewerId)
    {
        var viewed = s.UserId == viewerId || s.Views.Any(v => v.ViewerUserId == viewerId);
        return new StoryDto
        {
            StoryId = s.Id,
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            ViewedByMe = viewed,
            Items = s.Items.OrderBy(i => i.SortOrder).Select(i => new StoryItemDto
            {
                Id = i.Id,
                MediaType = i.MediaType,
                ObjectKey = i.ObjectKey,
                MediaUrl = _fileStorage.ResolveClientUrl(i.ObjectKey),
                DurationMs = i.DurationMs,
                TextOverlayJson = i.TextOverlayJson,
                SortOrder = i.SortOrder
            }).ToList()
        };
    }
}

public class GetUserStoriesQueryHandler : IRequestHandler<GetUserStoriesQuery, List<StoryDto>>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IFileStorageService _fileStorage;

    public GetUserStoriesQueryHandler(
        IStoryRepository storyRepository,
        IContactRepository contactRepository,
        IFileStorageService fileStorage)
    {
        _storyRepository = storyRepository;
        _contactRepository = contactRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<StoryDto>> Handle(GetUserStoriesQuery request, CancellationToken cancellationToken)
    {
        if (request.ViewerUserId != request.TargetUserId)
        {
            var contacts = await _contactRepository.GetContactsByUserIdAsync(request.ViewerUserId);
            var allowed = contacts.Any(c =>
                c.Status == ContactStatus.Accepted &&
                c.ContactUserId == request.TargetUserId);
            if (!allowed)
                throw new UnauthorizedAccessException("You can only view stories from your contacts.");
        }

        var stories = await _storyRepository.GetActiveStoriesForUsersAsync(
            new[] { request.TargetUserId }, DateTime.UtcNow, cancellationToken);

        return stories.Select(s =>
        {
            var viewed = s.UserId == request.ViewerUserId || s.Views.Any(v => v.ViewerUserId == request.ViewerUserId);
            return new StoryDto
            {
                StoryId = s.Id,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                ViewedByMe = viewed,
                Items = s.Items.OrderBy(i => i.SortOrder).Select(i => new StoryItemDto
                {
                    Id = i.Id,
                    MediaType = i.MediaType,
                    ObjectKey = i.ObjectKey,
                    MediaUrl = _fileStorage.ResolveClientUrl(i.ObjectKey),
                    DurationMs = i.DurationMs,
                    TextOverlayJson = i.TextOverlayJson,
                    SortOrder = i.SortOrder
                }).ToList()
            };
        }).ToList();
    }
}

public class GetStoryViewersQueryHandler : IRequestHandler<GetStoryViewersQuery, List<StoryViewerDto>>
{
    private readonly IStoryRepository _storyRepository;
    private readonly IFileStorageService _fileStorage;

    public GetStoryViewersQueryHandler(IStoryRepository storyRepository, IFileStorageService fileStorage)
    {
        _storyRepository = storyRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<StoryViewerDto>> Handle(GetStoryViewersQuery request, CancellationToken cancellationToken)
    {
        var story = await _storyRepository.GetByIdWithItemsAsync(request.StoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Story not found.");

        if (story.UserId != request.RequesterId)
            throw new UnauthorizedAccessException("Only the author can see viewers.");

        var views = await _storyRepository.GetViewsForStoryAsync(request.StoryId, cancellationToken);
        return views.Select(v => new StoryViewerDto
        {
            UserId = v.ViewerUserId,
            DisplayName = v.ViewerUser != null
                ? $"{v.ViewerUser.FirstName} {v.ViewerUser.LastName}".Trim()
                : "User",
            ProfilePicture = _fileStorage.ResolveClientUrl(v.ViewerUser?.ProfilePicture),
            ViewedAt = v.ViewedAt
        }).ToList();
    }
}
