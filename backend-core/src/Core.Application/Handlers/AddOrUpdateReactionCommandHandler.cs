using System.Text.Json;
using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;
using Core.Domain.Entities;

namespace Core.Application.Handlers;

public class AddOrUpdateReactionCommandHandler : IRequestHandler<AddOrUpdateReactionCommand>
{
    public const string OutboxTypeReactionsUpdated = "MessageReactionsUpdated";

    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrUpdateReactionCommandHandler(
        IMessageRepository messageRepository,
        IMessageReactionRepository reactionRepository,
        IGroupRepository groupRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _groupRepository = groupRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddOrUpdateReactionCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
            throw new KeyNotFoundException("Message not found.");

        if (message.GroupId.HasValue)
        {
            var group = await _groupRepository.GetGroupWithMembersAsync(message.GroupId.Value);
            if (group == null || !group.Members.Any(m => m.UserId == request.UserId))
                throw new UnauthorizedAccessException("You are not a member of this group.");
        }
        else
        {
            if (message.SenderId != request.UserId && message.ReceiverId != request.UserId)
                throw new UnauthorizedAccessException("You are not a participant in this conversation.");
        }

        var existing = await _reactionRepository.GetByMessageAndUserAsync(request.MessageId, request.UserId);
        if (string.IsNullOrWhiteSpace(request.Emoji))
        {
            if (existing != null)
                _reactionRepository.Delete(existing);
        }
        else
        {
            if (existing == null)
            {
                var reaction = new MessageReaction(request.MessageId, request.UserId, request.Emoji);
                await _reactionRepository.AddAsync(reaction);
            }
            else
            {
                existing.SetEmoji(request.Emoji);
                _reactionRepository.Update(existing);
            }
        }

        var payload = JsonSerializer.Serialize(new MessageReactionsOutboxPayload(request.MessageId));
        await _outboxRepository.AddAsync(new OutboxMessage(OutboxTypeReactionsUpdated, payload), cancellationToken);
        await _unitOfWork.SaveChangesAsync();
    }
}

public sealed record MessageReactionsOutboxPayload(Guid MessageId);
