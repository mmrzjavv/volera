using MediatR;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class SyncMessagesQueryHandler : IRequestHandler<SyncMessagesQuery, SyncMessagesResultDto>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IFileStorageService _fileStorage;

    public SyncMessagesQueryHandler(
        IMessageRepository messageRepository,
        ISavedMessageRepository savedMessageRepository,
        IMessageReactionRepository reactionRepository,
        IGroupRepository groupRepository,
        IFileStorageService fileStorage)
    {
        _messageRepository = messageRepository;
        _savedMessageRepository = savedMessageRepository;
        _reactionRepository = reactionRepository;
        _groupRepository = groupRepository;
        _fileStorage = fileStorage;
    }

    public async Task<SyncMessagesResultDto> Handle(SyncMessagesQuery request, CancellationToken cancellationToken)
    {
        if (request.PeerUserId.HasValue == request.GroupId.HasValue)
            throw new InvalidOperationException("Provide exactly one of PeerUserId or GroupId.");

        var limit = Math.Clamp(request.Limit, 1, 200);
        IEnumerable<Core.Domain.Entities.Message> messages;

        if (request.GroupId.HasValue)
        {
            var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId.Value);
            if (group == null || group.Members.All(m => m.UserId != request.CurrentUserId))
                throw new UnauthorizedAccessException("Not a member of this group.");

            messages = await _messageRepository.SyncGroupMessagesAsync(
                request.GroupId.Value, request.AfterSentAt, request.AfterId, limit + 1, cancellationToken);
        }
        else
        {
            messages = await _messageRepository.SyncConversationAsync(
                request.CurrentUserId, request.PeerUserId!.Value, request.AfterSentAt, request.AfterId, limit + 1, cancellationToken);
        }

        var list = messages.ToList();
        var hasMore = list.Count > limit;
        if (hasMore) list = list.Take(limit).ToList();

        var savedMessageIds = await _savedMessageRepository.GetSavedMessageIdsAsync(request.CurrentUserId, list.Select(m => m.Id));
        var reactions = await _reactionRepository.GetByMessageIdsAsync(list.Select(m => m.Id));

        var dtos = list.Select(m =>
        {
            var messageReactions = reactions
                .Where(r => r.MessageId == m.Id)
                .Select(r => new MessageReactionDto
                {
                    UserId = r.UserId,
                    UserName = $"{r.User.FirstName} {r.User.LastName}".Trim(),
                    Emoji = r.Emoji
                })
                .ToList();

            return new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                GroupId = m.GroupId,
                Content = m.Content,
                AttachmentUrl = _fileStorage.ResolveClientUrl(m.AttachmentUrl),
                AttachmentType = m.AttachmentType,
                SentAt = m.SentAt,
                IsRead = m.IsRead,
                IsEdited = m.IsEdited,
                IsSaved = savedMessageIds.Contains(m.Id),
                DeletedAt = m.DeletedAt,
                ReplyToMessageId = m.ReplyToMessageId,
                ClientMessageId = m.ClientMessageId,
                ForwardedFromMessageId = m.ForwardedFromMessageId,
                ForwardedAt = m.ForwardedAt,
                IsPinned = m.IsPinned,
                PinnedAt = m.PinnedAt,
                PinnedByUserId = m.PinnedByUserId,
                Reactions = messageReactions
            };
        }).ToList();

        var last = dtos.LastOrDefault();
        return new SyncMessagesResultDto
        {
            Messages = dtos,
            HasMore = hasMore,
            NextAfterSentAt = last?.SentAt,
            NextAfterId = last?.Id
        };
    }
}
