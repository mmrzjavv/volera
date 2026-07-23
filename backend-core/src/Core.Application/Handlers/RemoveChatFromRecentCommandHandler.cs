using MediatR;
using Core.Application.Commands;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class RemoveChatFromRecentCommandHandler : IRequestHandler<RemoveChatFromRecentCommand>
{
    private readonly IHiddenChatRepository _hiddenChatRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveChatFromRecentCommandHandler(
        IHiddenChatRepository hiddenChatRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _hiddenChatRepository = hiddenChatRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveChatFromRecentCommand request, CancellationToken cancellationToken)
    {
        if (request.GroupId.HasValue)
        {
            await _mediator.Send(new LeaveGroupCommand
            {
                GroupId = request.GroupId.Value,
                UserId = request.CurrentUserId
            }, cancellationToken);
            return;
        }

        if (request.OtherUserId.HasValue && request.OtherUserId != request.CurrentUserId)
        {
            if (await _hiddenChatRepository.ExistsAsync(request.CurrentUserId, request.OtherUserId.Value, cancellationToken))
                return; // Already hidden

            var hiddenChat = new HiddenChat(request.CurrentUserId, request.OtherUserId.Value);
            await _hiddenChatRepository.AddAsync(hiddenChat, cancellationToken);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
