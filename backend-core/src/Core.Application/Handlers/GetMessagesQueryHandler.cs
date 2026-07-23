using MediatR;
using Core.Application.DTOs;
using Core.Application.Queries;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, List<MessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IFileStorageService _fileStorage;

    public GetMessagesQueryHandler(
        IMessageRepository messageRepository,
        ISavedMessageRepository savedMessageRepository,
        IMessageReactionRepository reactionRepository,
        IFileStorageService fileStorage)
    {
        _messageRepository = messageRepository;
        _savedMessageRepository = savedMessageRepository;
        _reactionRepository = reactionRepository;
        _fileStorage = fileStorage;
    }

    public async Task<List<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _messageRepository.GetConversationAsync(request.CurrentUserId, request.UserId, request.Limit, request.Before);
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
                ReplyToStoryItemId = m.ReplyToStoryItemId,
                ReplyToStoryItemPreview = m.ReplyToStoryItem == null
                    ? null
                    : new ReplyToStoryItemPreviewDto
                    {
                        StoryItemId = m.ReplyToStoryItem.Id,
                        StoryOwnerId = m.ReplyToStoryItem.Story?.UserId ?? Guid.Empty,
                        OwnerName = m.ReplyToStoryItem.Story?.User != null
                            ? $"{m.ReplyToStoryItem.Story.User.FirstName} {m.ReplyToStoryItem.Story.User.LastName}".Trim()
                            : "Story",
                        MediaUrl = _fileStorage.ResolveClientUrl(m.ReplyToStoryItem.ObjectKey),
                        MediaType = m.ReplyToStoryItem.MediaType,
                        OverlaySnippet = ExtractOverlaySnippet(m.ReplyToStoryItem.TextOverlayJson)
                    },
                ForwardedFromMessageId = m.ForwardedFromMessageId,
                ForwardedAt = m.ForwardedAt,
                IsPinned = m.IsPinned,
                PinnedAt = m.PinnedAt,
                PinnedByUserId = m.PinnedByUserId,
                Reactions = messageReactions
            };
        }).ToList();
    }

    private static string? ExtractOverlaySnippet(string? textOverlayJson)
    {
        if (string.IsNullOrWhiteSpace(textOverlayJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(textOverlayJson);
            if (doc.RootElement.TryGetProperty("text", out var textProp))
            {
                var text = textProp.GetString();
                if (string.IsNullOrWhiteSpace(text)) return null;
                return text.Length > 80 ? text[..77] + "..." : text;
            }
        }
        catch
        {
            // ignore malformed overlay json
        }
        return null;
    }
}
