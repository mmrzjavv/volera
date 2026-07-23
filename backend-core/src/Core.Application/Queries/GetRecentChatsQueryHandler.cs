using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.DTOs;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Queries;

public class GetRecentChatsQueryHandler : IRequestHandler<GetRecentChatsQuery, IEnumerable<RecentChatDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IOnlineUserService _onlineUserService;
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IHiddenChatRepository _hiddenChatRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<GetRecentChatsQueryHandler> _logger;

    public GetRecentChatsQueryHandler(
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IOnlineUserService onlineUserService,
        ISavedMessageRepository savedMessageRepository,
        IHiddenChatRepository hiddenChatRepository,
        IFileStorageService fileStorage,
        ILogger<GetRecentChatsQueryHandler> logger)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _onlineUserService = onlineUserService;
        _savedMessageRepository = savedMessageRepository;
        _hiddenChatRepository = hiddenChatRepository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<IEnumerable<RecentChatDto>> Handle(GetRecentChatsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching recent chats for User {UserId}.", request.UserId);
        var rawChats = (await _messageRepository.GetRecentChatsAsync(request.UserId)).ToList();
        _logger.LogInformation("User {UserId}: found {RawChatCount} raw recent chat(s).", request.UserId, rawChats.Count);

        var result = new List<RecentChatDto>();

        // Always expose "Saved Messages" chat, even if user has not saved anything yet
        var savedCount = await _savedMessageRepository.GetCountByUserIdAsync(request.UserId);
        var latestSaved = savedCount > 0
            ? (await _savedMessageRepository.GetByUserIdAsync(request.UserId, 1, 1)).FirstOrDefault()
            : null;

        result.Add(new RecentChatDto
        {
            UserId = request.UserId, // Use current user ID for Saved Messages
            Name = "Saved Messages",
            FirstName = "Saved",
            LastName = "Messages",
            Username = "savedmessages", // Special username
            ProfilePicture = null, // Can be handled by frontend
            LastMessageContent = latestSaved?.Message?.Content ?? "No saved messages yet",
            LastMessageAt = latestSaved?.SavedAt ?? DateTime.MinValue,
            UnreadCount = 0, // Saved messages are always read
            IsOnline = true, // Always online
            IsGroup = false
        });

        if (!rawChats.Any())
        {
            return result;
        }

        var hiddenUserIds = await _hiddenChatRepository.GetHiddenUserIdsAsync(request.UserId, cancellationToken);

        var userIds = rawChats
            .Where(c => c.OtherUserId.HasValue && c.OtherUserId != Guid.Empty && c.OtherUserId != request.UserId)
            .Select(c => c.OtherUserId!.Value)
            .Distinct()
            .ToList();

        var groupIds = rawChats
            .Where(c => c.GroupId.HasValue)
            .Select(c => c.GroupId!.Value)
            .Distinct()
            .ToList();

        // Batch-load users and groups to avoid N+1
        var users = await _userRepository.GetUsersByIdsAsync(userIds, cancellationToken);
        var userLookup = users.ToDictionary(u => u.Id);

        var groups = await _groupRepository.GetGroupsByIdsAsync(groupIds, cancellationToken);
        var groupLookup = groups.ToDictionary(g => g.Id);

        // Batch-load online statuses
        var onlineUserIds = await _onlineUserService.GetOnlineUserIds();
        var onlineSet = onlineUserIds.ToHashSet();

        foreach (var chat in rawChats)
        {
            // Skip if it's a self-chat (Sender=Receiver=Me) because we handle it via Saved Messages
            if (chat.OtherUserId.HasValue && chat.OtherUserId == request.UserId)
            {
                continue;
            }

            // Skip hidden direct chats
            if (chat.OtherUserId.HasValue && hiddenUserIds.Contains(chat.OtherUserId.Value))
            {
                continue;
            }

            if (chat.GroupId.HasValue && groupLookup.TryGetValue(chat.GroupId.Value, out var group))
            {
                var isChannel = group.Kind == Core.Domain.Enums.GroupKind.Channel;
                result.Add(new RecentChatDto
                {
                    GroupId = group.Id,
                    Name = group.Name,
                    IsGroup = !isChannel,
                    IsChannel = isChannel,
                    ProfilePicture = _fileStorage.ResolveClientUrl(group.ProfilePictureUrl),
                    PublicUsername = group.PublicUsername,
                    LastMessageContent = chat.LastMessage?.Content ?? string.Empty,
                    LastMessageAt = chat.LastMessage?.SentAt ?? DateTime.MinValue,
                    UnreadCount = chat.UnreadCount
                });
            }
            else if (chat.OtherUserId.HasValue && chat.OtherUserId != Guid.Empty &&
                     userLookup.TryGetValue(chat.OtherUserId.Value, out var user))
            {
                var isOnline = onlineSet.Contains(user.Id);

                result.Add(new RecentChatDto
                {
                    UserId = user.Id,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    ProfilePicture = _fileStorage.ResolveClientUrl(user.ProfilePicture),
                    LastMessageContent = chat.LastMessage?.Content ?? string.Empty,
                    LastMessageAt = chat.LastMessage?.SentAt ?? DateTime.MinValue,
                    UnreadCount = chat.UnreadCount,
                    IsOnline = isOnline,
                    IsGroup = false
                });
            }
        }

        return result.OrderByDescending(x => x.LastMessageAt);
    }
}
