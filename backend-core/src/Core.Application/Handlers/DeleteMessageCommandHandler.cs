using System.Text.Json;
using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Commands;
using Core.Domain.Entities;

namespace Core.Application.Handlers;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, bool>
{
    public const string OutboxTypeMessageDeleted = "MessageDeleted";

    private readonly IMessageRepository _messageRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMessageCommandHandler(
        IMessageRepository messageRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
            return false;

        if (message.SenderId != request.UserId)
            throw new UnauthorizedAccessException("You can only delete your own messages.");

        message.Delete();
        message.ClearDomainEvents();
        _messageRepository.Update(message);

        var payload = JsonSerializer.Serialize(new MessageDeletedOutboxPayload(
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.GroupId,
            DateTime.UtcNow));

        await _outboxRepository.AddAsync(new OutboxMessage(OutboxTypeMessageDeleted, payload), cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public sealed record MessageDeletedOutboxPayload(
    Guid MessageId,
    Guid SenderId,
    Guid? ReceiverId,
    Guid? GroupId,
    DateTime DeletedAt);
