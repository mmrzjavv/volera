using System.Text.Json;
using MediatR;
using Core.Domain.Interfaces;
using Core.Application.Commands;
using Core.Domain.Entities;

namespace Core.Application.Handlers;

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, bool>
{
    public const string OutboxTypeMessageEdited = "MessageEdited";

    private readonly IMessageRepository _messageRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditMessageCommandHandler(
        IMessageRepository messageRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _messageRepository = messageRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
            return false;

        if (message.SenderId != request.UserId)
            throw new UnauthorizedAccessException("You can only edit your own messages.");

        message.Edit(request.NewContent);
        message.ClearDomainEvents();
        _messageRepository.Update(message);

        var editedAt = DateTime.UtcNow;
        var payload = JsonSerializer.Serialize(new MessageEditedOutboxPayload(
            message.Id,
            message.SenderId,
            message.ReceiverId,
            message.GroupId,
            message.Content,
            editedAt));

        await _outboxRepository.AddAsync(new OutboxMessage(OutboxTypeMessageEdited, payload), cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}

public sealed record MessageEditedOutboxPayload(
    Guid MessageId,
    Guid SenderId,
    Guid? ReceiverId,
    Guid? GroupId,
    string NewContent,
    DateTime EditedAt);
