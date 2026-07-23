using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Interfaces;
using MediatR;

namespace Core.Application.Handlers;

public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, bool>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageNotificationService _notificationService;

    public MarkMessagesAsReadCommandHandler(
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IMessageNotificationService notificationService)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
    {
        await _messageRepository.MarkAsReadAsync(request.UserId, request.SenderId);
        await _unitOfWork.SaveChangesAsync();

        // Notify the sender that their messages have been read
        await _notificationService.NotifyMessagesRead(request.UserId, request.SenderId);

        return true;
    }
}
