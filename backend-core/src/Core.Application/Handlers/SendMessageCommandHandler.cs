using System.Text.Json;
using MediatR;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Domain.Interfaces;
using Core.Application.Commands;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
{
    public const string OutboxTypeMessageSent = "MessageSent";

    private readonly IMessageRepository _messageRepository;
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILimitResolutionService _limitResolutionService;

    public SendMessageCommandHandler(
        IMessageRepository messageRepository,
        ISavedMessageRepository savedMessageRepository,
        IOutboxRepository outboxRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILimitResolutionService limitResolutionService)
    {
        _messageRepository = messageRepository;
        _savedMessageRepository = savedMessageRepository;
        _outboxRepository = outboxRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _limitResolutionService = limitResolutionService;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (request.ClientMessageId.HasValue && request.ClientMessageId.Value != Guid.Empty)
        {
            var existing = await _messageRepository.GetBySenderAndClientMessageIdAsync(
                request.SenderId, request.ClientMessageId.Value, cancellationToken);
            if (existing != null)
                return existing.Id;
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            var maxLength = await _limitResolutionService.GetEffectiveLimitAsync(request.SenderId, LimitKeys.MaxMessageLength, cancellationToken);
            if (maxLength > 0 && request.Content.Length > (int)maxLength)
            {
                throw new InvalidOperationException($"Message exceeds maximum length of {maxLength} characters. Please split your message into smaller parts.");
            }
        }

        Group? conversation = null;
        if (request.GroupId.HasValue)
        {
            conversation = await _groupRepository.GetGroupWithMembersAsync(request.GroupId.Value)
                ?? throw new KeyNotFoundException("Group or channel not found.");

            if (!conversation.IsMember(request.SenderId))
            {
                if (conversation.Kind == GroupKind.Group)
                {
                    var linkedChannel = await _groupRepository.GetChannelByLinkedDiscussionGroupIdAsync(conversation.Id, cancellationToken);
                    if (linkedChannel != null && linkedChannel.IsMember(request.SenderId))
                        conversation.AddMember(request.SenderId, false);
                    else
                        throw new UnauthorizedAccessException("You are not a member of this group.");
                }
                else
                {
                    throw new UnauthorizedAccessException("You are not a member of this conversation.");
                }
            }

            if (conversation.Kind == GroupKind.Channel && !conversation.CanUserPost(request.SenderId))
                throw new UnauthorizedAccessException("Only channel admins can post.");
        }

        if (request.ReplyToMessageId.HasValue)
        {
            var replyToMessage = await _messageRepository.GetByIdAsync(request.ReplyToMessageId.Value)
                ?? throw new KeyNotFoundException("Replied message not found.");

            if (request.GroupId.HasValue)
            {
                var sameGroup = replyToMessage.GroupId == request.GroupId.Value;
                var isLinkedDiscussion = false;
                if (!sameGroup && replyToMessage.GroupId.HasValue)
                {
                    var channel = await _groupRepository.GetGroupWithMembersAsync(replyToMessage.GroupId.Value);
                    isLinkedDiscussion = channel?.Kind == GroupKind.Channel
                        && channel.LinkedDiscussionGroupId == request.GroupId.Value;
                }

                if (!sameGroup && !isLinkedDiscussion)
                    throw new InvalidOperationException("Cannot reply to a message from a different group.");
            }
            else if (request.ReceiverId.HasValue)
            {
                var repliedParticipants = new HashSet<Guid>
                {
                    replyToMessage.SenderId,
                    replyToMessage.ReceiverId ?? Guid.Empty
                };
                var currentParticipants = new HashSet<Guid>
                {
                    request.SenderId,
                    request.ReceiverId.Value
                };
                repliedParticipants.Remove(Guid.Empty);
                if (!repliedParticipants.SetEquals(currentParticipants))
                    throw new InvalidOperationException("Cannot reply to a message from a different conversation.");
            }
        }

        Group? sendAsChannel = null;
        if (request.SendAsChannelId.HasValue)
        {
            sendAsChannel = await _groupRepository.GetGroupWithMembersAsync(request.SendAsChannelId.Value)
                ?? throw new KeyNotFoundException("Send-as channel not found.");
            if (sendAsChannel.Kind != GroupKind.Channel || !sendAsChannel.IsPublic)
                throw new InvalidOperationException("Can only post as a public channel.");
            if (!sendAsChannel.CanUserPost(request.SenderId))
                throw new UnauthorizedAccessException("You cannot post as this channel.");
        }

        Message message;
        if (request.GroupId.HasValue)
        {
            message = new Message(
                request.SenderId,
                request.GroupId.Value,
                request.Content,
                true,
                request.AttachmentUrl,
                request.AttachmentType,
                request.ReplyToMessageId);
        }
        else if (request.ReceiverId.HasValue)
        {
            message = new Message(
                request.SenderId,
                request.ReceiverId.Value,
                request.Content,
                request.AttachmentUrl,
                request.AttachmentType,
                request.ReplyToMessageId);
        }
        else
        {
            throw new InvalidOperationException("SendMessageCommand must have either ReceiverId or GroupId set.");
        }

        if (request.ClientMessageId.HasValue && request.ClientMessageId.Value != Guid.Empty)
            message.AssignClientMessageId(request.ClientMessageId.Value);

        if (request.ReplyToStoryItemId.HasValue && request.ReplyToStoryItemId.Value != Guid.Empty)
            message.SetReplyToStoryItem(request.ReplyToStoryItemId.Value);

        if (sendAsChannel != null)
            message.SetSendAsChannel(sendAsChannel.Id);

        if (conversation?.Kind == GroupKind.Channel && conversation.SignaturesEnabled)
        {
            var sender = await _userRepository.GetByIdAsync(request.SenderId);
            var display = sender != null ? $"{sender.FirstName} {sender.LastName}".Trim() : null;
            if (!string.IsNullOrWhiteSpace(display))
                message.SetSignature(display);
        }

        message.ClearDomainEvents();
        await _messageRepository.AddAsync(message);

        var payload = JsonSerializer.Serialize(new MessageSentOutboxPayload(
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.GroupId,
            message.Content,
            message.SentAt,
            message.AttachmentUrl,
            message.AttachmentType,
            message.BranchId,
            message.ReplyToMessageId,
            message.SupportSenderId));

        await _outboxRepository.AddAsync(new OutboxMessage(OutboxTypeMessageSent, payload), cancellationToken);

        if (request.ReceiverId.HasValue && request.SenderId == request.ReceiverId.Value)
        {
            await _savedMessageRepository.AddAsync(new SavedMessage(request.SenderId, message.Id));
        }

        if (conversation?.Kind == GroupKind.Channel && conversation.LinkedDiscussionGroupId.HasValue)
        {
            var seedContent = string.IsNullOrEmpty(request.Content) || request.Content.Length <= 200
                ? $"Discussion: {request.Content}"
                : $"Discussion: {request.Content[..200]}…";
            var seed = new Message(
                request.SenderId,
                conversation.LinkedDiscussionGroupId.Value,
                seedContent,
                true,
                null,
                null,
                message.Id);
            seed.ClearDomainEvents();
            await _messageRepository.AddAsync(seed);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception) when (request.ClientMessageId.HasValue)
        {
            var raced = await _messageRepository.GetBySenderAndClientMessageIdAsync(
                request.SenderId, request.ClientMessageId.Value, cancellationToken);
            if (raced != null)
                return raced.Id;
            throw;
        }

        return message.Id;
    }
}

public sealed record MessageSentOutboxPayload(
    Guid MessageId,
    Guid SenderId,
    Guid? ReceiverId,
    Guid? GroupId,
    string Content,
    DateTime SentAt,
    string? AttachmentUrl,
    string? AttachmentType,
    Guid? BranchId,
    Guid? ReplyToMessageId,
    Guid? SupportSenderId);
