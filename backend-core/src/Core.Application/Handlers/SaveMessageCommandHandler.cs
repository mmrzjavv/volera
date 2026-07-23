using MediatR;
using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Application.Commands;
using Core.Application.Interfaces;

namespace Core.Application.Handlers;

public class SaveMessageCommandHandler : IRequestHandler<SaveMessageCommand, Guid>
{
    private readonly ISavedMessageRepository _savedMessageRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILimitResolutionService _limitResolutionService;

    public SaveMessageCommandHandler(
        ISavedMessageRepository savedMessageRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        ILimitResolutionService limitResolutionService)
    {
        _savedMessageRepository = savedMessageRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _limitResolutionService = limitResolutionService;
    }

    public async Task<Guid> Handle(SaveMessageCommand request, CancellationToken cancellationToken)
    {
        // Check if already saved
        var existing = await _savedMessageRepository.GetByUserAndMessageIdAsync(request.UserId, request.MessageId);
        if (existing != null)
        {
            return existing.Id;
        }

        // Verify message exists
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message == null)
        {
            throw new KeyNotFoundException($"Message with ID {request.MessageId} not found.");
        }

        // Enforce MaxSavedMessagesCount limit
        var maxCount = await _limitResolutionService.GetEffectiveLimitAsync(request.UserId, LimitKeys.MaxSavedMessagesCount, cancellationToken);
        if (maxCount > 0)
        {
            var currentCount = await _savedMessageRepository.GetCountByUserIdAsync(request.UserId);
            if (currentCount >= (int)maxCount)
            {
                throw new InvalidOperationException("Saved messages limit reached. You have reached the maximum number of saved messages.");
            }
        }

        var savedMessage = new SavedMessage(request.UserId, request.MessageId);
        await _savedMessageRepository.AddAsync(savedMessage);
        await _unitOfWork.SaveChangesAsync();

        return savedMessage.Id;
    }
}
