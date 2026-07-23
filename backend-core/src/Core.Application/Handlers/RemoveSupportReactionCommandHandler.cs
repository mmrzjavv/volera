using MediatR;
using Core.Application.Commands;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace Core.Application.Handlers;

public class RemoveSupportReactionCommandHandler : IRequestHandler<RemoveSupportReactionCommand>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly ISupportUserBranchRepository _supportUserBranchRepository;
    private readonly ISupportUserRepository _supportUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageNotificationService _notificationService;

    public RemoveSupportReactionCommandHandler(
        IMessageRepository messageRepository,
        IMessageReactionRepository reactionRepository,
        ISupportUserBranchRepository supportUserBranchRepository,
        ISupportUserRepository supportUserRepository,
        IUnitOfWork unitOfWork,
        IMessageNotificationService notificationService)
    {
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _supportUserBranchRepository = supportUserBranchRepository;
        _supportUserRepository = supportUserRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task Handle(RemoveSupportReactionCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
            throw new KeyNotFoundException("Message not found.");

        if (!message.BranchId.HasValue)
            throw new UnauthorizedAccessException("This message is not a branch message.");

        var supportUser = await _supportUserRepository.GetByIdAsync(request.SupportUserId);
        if (supportUser == null || !supportUser.IsActive)
            throw new UnauthorizedAccessException("Support user not found or inactive.");

        if (supportUser.CompanyId != message.CompanyId)
            throw new UnauthorizedAccessException("You do not have access to this branch.");

        if (!supportUser.Role.CanViewAllCompanyMessages())
        {
            var assignment = await _supportUserBranchRepository.GetBySupportUserIdAndBranchIdAsync(request.SupportUserId, message.BranchId.Value, cancellationToken);
            if (assignment == null)
                throw new UnauthorizedAccessException("You are not assigned to this branch.");
        }

        var existing = await _reactionRepository.GetByMessageAndSupportUserAsync(request.MessageId, request.SupportUserId);
        if (existing != null)
        {
            _reactionRepository.Delete(existing);
            await _unitOfWork.SaveChangesAsync();
            await _notificationService.NotifyMessageReactionsUpdated(request.MessageId);
        }
    }
}
