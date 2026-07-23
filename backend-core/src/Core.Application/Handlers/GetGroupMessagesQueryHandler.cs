using MediatR;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetGroupMessagesQueryHandler : IRequestHandler<GetGroupMessagesQuery, List<MessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IFileStorageService _fileStorage;

    public GetGroupMessagesQueryHandler(
        IMessageRepository messageRepository,
        IGroupRepository groupRepository,
        ISavedMessageRepository savedMessageRepository,
        IMessageReactionRepository reactionRepository,
        IFileStorageService fileStorage)
    {
        _messageRepository = messageRepository;
        _groupRepository = groupRepository;
        _savedMessageRepository = savedMessageRepository;
        _reactionRepository = reactionRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<MessageDto>> Handle(GetGroupMessagesQuery request, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
            throw new KeyNotFoundException("Group not found.");

        if (!group.Members.Any(m => m.UserId == request.CurrentUserId))
            throw new UnauthorizedAccessException("You are not a member of this group.");

        var messages = await _messageRepository.GetGroupMessagesAsync(request.GroupId, request.Limit, request.Before);
        var savedMessageIds = await _savedMessageRepository.GetSavedMessageIdsAsync(request.CurrentUserId, messages.Select(m => m.Id));
        var reactions = await _reactionRepository.GetByMessageIdsAsync(messages.Select(m => m.Id));

        return messages.Select(m =>
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
                ReplyToMessagePreview = m.ReplyToMessage == null
                    ? null
                    : new ReplyToMessagePreviewDto
                    {
                        Id = m.ReplyToMessage.Id,
                        SenderId = m.ReplyToMessage.SenderId,
                        SenderName = $"{m.ReplyToMessage.Sender?.FirstName} {m.ReplyToMessage.Sender?.LastName}".Trim(),
                        ContentSnippet = string.IsNullOrEmpty(m.ReplyToMessage.Content)
                            ? string.Empty
                            : (m.ReplyToMessage.Content.Length > 80
                                ? m.ReplyToMessage.Content.Substring(0, 77) + "..."
                                : m.ReplyToMessage.Content),
                        DeletedAt = m.ReplyToMessage.DeletedAt
                    },
                ForwardedFromMessageId = m.ForwardedFromMessageId,
                ForwardedAt = m.ForwardedAt,
                IsPinned = m.IsPinned,
                PinnedAt = m.PinnedAt,
                PinnedByUserId = m.PinnedByUserId,
                Reactions = messageReactions,
                ClientMessageId = m.ClientMessageId,
                SignatureDisplayName = m.SignatureDisplayName,
                ViewCount = m.ViewCount,
                SendAsChannelId = m.SendAsChannelId
            };
        }).ToList();
    }
}
