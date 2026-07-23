using AutoMapper;
using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// EF Core-based implementation of <see cref="IMessageReadModelService"/> that uses
/// AutoMapper for efficient projection to DTOs. All data access is contained here,
/// keeping WebAPI purely focused on transport and signaling concerns.
/// </summary>
public class MessageReadModelService : IMessageReadModelService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;

    public MessageReadModelService(ApplicationDbContext dbContext, IMapper mapper, IFileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _fileStorage = fileStorage;
    }

    public async Task<MessageDto> BuildMessageDtoForNotificationAsync(
        Guid messageId,
        Guid senderId,
        Guid? receiverId,
        Guid? groupId,
        string content,
        DateTime sentAt,
        string? attachmentUrl,
        string? attachmentType,
        CancellationToken cancellationToken = default)
    {
        // Try to load the full message (including reply information) for richer notifications.
        var message = await _dbContext.Messages
            .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is not null)
        {
            var dto = _mapper.Map<MessageDto>(message);
            dto.AttachmentUrl = _fileStorage.ResolveClientUrl(dto.AttachmentUrl);

            if (message.ReplyToMessage is not null)
            {
                dto.ReplyToMessagePreview = new ReplyToMessagePreviewDto
                {
                    Id = message.ReplyToMessage.Id,
                    SenderId = message.ReplyToMessage.SenderId,
                    SenderName = $"{message.ReplyToMessage.Sender?.FirstName} {message.ReplyToMessage.Sender?.LastName}".Trim(),
                    ContentSnippet = BuildContentSnippet(message.ReplyToMessage.Content),
                    DeletedAt = message.ReplyToMessage.DeletedAt
                };
            }

            if (message.ReplyToStoryItemId.HasValue)
            {
                dto.ReplyToStoryItemId = message.ReplyToStoryItemId;
                var item = await _dbContext.StoryItems
                    .Include(i => i.Story).ThenInclude(s => s.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == message.ReplyToStoryItemId.Value, cancellationToken);
                if (item != null)
                {
                    string? overlay = null;
                    if (!string.IsNullOrWhiteSpace(item.TextOverlayJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(item.TextOverlayJson);
                            if (doc.RootElement.TryGetProperty("text", out var t))
                                overlay = t.GetString();
                        }
                        catch { /* ignore */ }
                    }
                    dto.ReplyToStoryItemPreview = new ReplyToStoryItemPreviewDto
                    {
                        StoryItemId = item.Id,
                        StoryOwnerId = item.Story?.UserId ?? Guid.Empty,
                        OwnerName = item.Story?.User != null
                            ? $"{item.Story.User.FirstName} {item.Story.User.LastName}".Trim()
                            : "Story",
                        MediaUrl = _fileStorage.ResolveClientUrl(item.ObjectKey),
                        MediaType = item.MediaType,
                        OverlaySnippet = overlay
                    };
                }
            }

            // Real-time path does not need "saved" flag; list views can compute it.
            dto.IsSaved = false;

            return dto;
        }

        // Fallback to a minimal payload if the message isn't yet available in the database.
        return new MessageDto
        {
            Id = messageId,
            SenderId = senderId,
            ReceiverId = receiverId,
            GroupId = groupId,
            Content = content,
            SentAt = sentAt,
            IsRead = false,
            AttachmentUrl = _fileStorage.ResolveClientUrl(attachmentUrl),
            AttachmentType = attachmentType
        };
    }

    public async Task<MessageReactionsUpdatedPayload?> BuildMessageReactionsUpdatedPayloadAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is null)
        {
            return null;
        }

        var reactions = await _dbContext.MessageReactions
            .Include(r => r.User)
            .Include(r => r.SupportUser)
            .Where(r => r.MessageId == messageId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reactionDtos = reactions
            .Select(r => new MessageReactionDto
            {
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}".Trim() : null,
                SupportUserId = r.SupportUserId,
                SupportUserName = r.SupportUser != null ? $"{r.SupportUser.FirstName} {r.SupportUser.LastName}".Trim() : (r.SupportUser?.Username ?? null),
                Emoji = r.Emoji
            })
            .ToList();

        var participantUserIds = GetParticipantIds(message);
        var branchId = message.BranchId;
        var branchSenderId = message.BranchId.HasValue ? message.SenderId : (Guid?)null;

        return new MessageReactionsUpdatedPayload(
            MessageId: messageId,
            GroupId: message.GroupId,
            BranchId: branchId,
            BranchMessageSenderId: branchSenderId,
            ParticipantUserIds: participantUserIds,
            Reactions: reactionDtos);
    }

    public async Task<MessagePinnedUpdatedPayload?> BuildMessagePinnedUpdatedPayloadAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is null)
        {
            return null;
        }

        var participantUserIds = GetParticipantIds(message);

        return new MessagePinnedUpdatedPayload(
            MessageId: message.Id,
            GroupId: message.GroupId,
            ParticipantUserIds: participantUserIds,
            IsPinned: message.IsPinned,
            PinnedAt: message.PinnedAt,
            PinnedByUserId: message.PinnedByUserId);
    }

    private static IReadOnlyCollection<Guid> GetParticipantIds(Message message)
    {
        if (message.GroupId.HasValue)
        {
            // For group messages, clients typically use the group id, but we still supply
            // an empty participant list so the caller can branch on GroupId.
            return Array.Empty<Guid>();
        }

        var participants = new[] { message.SenderId, message.ReceiverId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return participants;
    }

    private static string BuildContentSnippet(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        return content.Length > 80
            ? content[..77] + "..."
            : content;
    }
}

