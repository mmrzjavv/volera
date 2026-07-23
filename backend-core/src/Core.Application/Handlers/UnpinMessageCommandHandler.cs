using MediatR;
using Core.Application.Commands;
using Core.Domain.Interfaces;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class UnpinMessageCommandHandler : IRequestHandler<UnpinMessageCommand>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageNotificationService _notificationService;

    public UnpinMessageCommandHandler(
        IMessageRepository messageRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork,
        IMessageNotificationService notificationService)
    {
        _messageRepository = messageRepository;
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task Handle(UnpinMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
            throw new KeyNotFoundException("Message not found.");

        // Authorization: user must be part of the conversation
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

        message.Unpin();
        _messageRepository.Update(message);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.NotifyMessagePinnedUpdated(request.MessageId);
    }
}

